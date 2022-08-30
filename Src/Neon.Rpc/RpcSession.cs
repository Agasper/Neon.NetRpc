using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Neon.Rpc.Events;
using Neon.Rpc.Payload;
using Neon.Util;

namespace Neon.Rpc
{
    public abstract class RpcSession : RpcSessionBase, IRpcSession
    {
        /// <summary>
        /// Is session closed or not
        /// </summary>
        public bool IsClosed => closed;

        readonly Dictionary<uint, RemotingRequest> requests;
        readonly bool orderedExecution;
        readonly int defaultExecutionTimeout;
        readonly int orderedExecutionMaxQueue;

        readonly object orderedExecutionTaskMutex = new object();
        readonly object lastRequestIdMutex = new object();
        readonly TaskScheduler taskScheduler;

        Task orderedExecutionTask;
        uint lastRequestId;
        volatile int executionQueueSize;
        volatile bool closed;

        private protected readonly RemotingInvocationRules remotingInvocationRules;

        public RpcSession(RpcSessionContext sessionContext)
        : base(sessionContext)
        {
            if (sessionContext.TaskScheduler == null)
                throw new ArgumentNullException(nameof(sessionContext.TaskScheduler));
            
            this.requests = new Dictionary<uint, RemotingRequest>();
            this.orderedExecution = sessionContext.OrderedExecution;
            this.orderedExecutionTask = Task.CompletedTask;
            this.orderedExecutionMaxQueue = sessionContext.OrderedExecutionMaxQueue;
            this.remotingInvocationRules = sessionContext.RemotingInvocationRules;
            this.taskScheduler = sessionContext.TaskScheduler;
            this.defaultExecutionTimeout = sessionContext.DefaultExecutionTimeout;
        }

        /// <summary>
        /// Closing the session, causes all awaiting requests are cancelled and connection closed
        /// </summary>
        public void Close()
        {
            if (closed)
                return;
            closed = true;
            
            connection.Close();

            lock (requests)
            {
                foreach (var pair in requests)
                {
                    pair.Value.SetError(new RemotingException($"{this.GetType().Name} was closed prematurely!", RemotingException.StatusCodeEnum.ConnectionIssues));
                }

                requests.Clear();
            }

            try
            {
                OnClose(new OnCloseEventArgs());
            }
            catch (Exception e)
            {
                logger.Error($"{LogsSign} got an unhandled exception on {this.GetType().Name}.{nameof(OnClose)}: {e}");
            }

            this.logger.Debug($"{LogsSign} {this} closed!");
        }

        protected virtual void OnClose(OnCloseEventArgs args)
        {
            
        }

        void CheckConnected()
        {
            if (!connection.Connected)
                throw new InvalidOperationException($"{nameof(RpcSession)} is not established");
        }

        private protected override void RemotingRequest(RemotingRequest remotingRequest)
        {
            EnqueueRequest(remotingRequest);
        }

        private protected override void RemotingResponse(RemotingResponse remotingResponse)
        {
            ExecuteResponse(remotingResponse);
        }

        private protected override void RemotingResponseError(RemotingResponseError remotingResponseError)
        {
            ExecuteResponseError(remotingResponseError);
        }

        private protected override void AuthenticationRequest(AuthenticationRequest authenticationRequest)
        {
            AuthenticationResponse authenticationResponse = new AuthenticationResponse();
            authenticationResponse.Exception = new RemotingException("Authentication is not implemented",
                RemotingException.StatusCodeEnum.AccessDenied);
            _ = SendNeonMessage(authenticationResponse);
        }

        void ExecuteResponseError(RemotingResponseError remotingResponseError)
        {
            RemotingRequest remotingRequest = null;
            lock (requests)
            {
                if (!requests.TryGetValue(remotingResponseError.RequestId, out remotingRequest))
                {
                    logger.Warn($"{LogsSign} got response error for unknown request id {remotingResponseError.RequestId}");
                    return;
                }
                
                requests.Remove(remotingResponseError.RequestId);
            }
            
            remotingRequest.SetError(remotingResponseError.Exception);
            ProcessRemoteExecutionExceptionInternal(remotingRequest, remotingResponseError.Exception);
        }

        void ExecuteResponse(RemotingResponse remotingResponse)
        {
            RemotingRequest remotingRequest = null;
            lock (requests)
            {
                if (!requests.TryGetValue(remotingResponse.RequestId, out remotingRequest))
                {
                    logger.Warn($"{LogsSign} got response error for unknown request id {remotingResponse.RequestId}");
                    return;
                }
                
                requests.Remove(remotingResponse.RequestId);
            }

            remotingRequest.SetResult(remotingResponse);
        }

        void EnqueueRequest(RemotingRequest request)
        {
            if (orderedExecution && executionQueueSize >= orderedExecutionMaxQueue)
            {
                ProcessLocalExecutionExceptionInternal(request, new RemotingException("Execution queue exceed it's limits", RemotingException.StatusCodeEnum.QueueExceeded));
                return;
            }

            if (!orderedExecution)
            {
                Task.Factory.StartNew(() => ExecuteRequestInternalAsync(request), 
                    default, TaskCreationOptions.None,
                    taskScheduler ?? TaskScheduler.Default);
            }
            else
            {
                lock (orderedExecutionTaskMutex)
                {
                    Interlocked.Increment(ref executionQueueSize);
                    orderedExecutionTask = orderedExecutionTask.ContinueWith((t, o)
                        => ExecuteRequestInternalAsync(o as RemotingRequest), request,
                            default,
                            TaskContinuationOptions.ExecuteSynchronously,
                            taskScheduler ?? TaskScheduler.Default).Unwrap();
                }
            }
        }
        
        async Task ExecuteRequestInternalAsync(RemotingRequest request)
        {
            try
            {
                if (this.closed)
                {
                    logger.Debug($"{LogsSign} dropping incoming request {request}, connection already closed");
                    return;
                }

                logger.Debug($"{LogsSign} executing {request} locally");

                ExecutionRequest executionRequest = new ExecutionRequest(request);

                LocalExecutionStartingEventArgs eventArgsStarting =
                    new LocalExecutionStartingEventArgs(executionRequest);
                OnLocalExecutionStarting(eventArgsStarting);

                Stopwatch sw = new Stopwatch();
                sw.Start();
                var executionResponse = await ExecuteRequestAsync(new ExecutionRequest(request)).ConfigureAwait(false);
                sw.Stop();


                double timeSeconds = sw.ElapsedTicks / (double) Stopwatch.Frequency;
                ulong timeSpanTicks = (ulong) (timeSeconds * TimeSpan.TicksPerSecond);
                float ms = timeSpanTicks / (float) TimeSpan.TicksPerMillisecond;
                logger.Info($"{LogsSign} executed {request} locally in {ms.ToString("0.00")}ms");

                LocalExecutionCompletedEventArgs eventArgsCompleted =
                    new LocalExecutionCompletedEventArgs(executionRequest, executionResponse, ms);
                OnLocalExecutionCompleted(eventArgsCompleted);

                if (request.ExpectResponse)
                {
                    RemotingResponse response = new RemotingResponse();
                    response.RequestId = request.RequestId;
                    response.ExecutionTime = timeSpanTicks;
                    response.HasArgument = false;
                    if (request.ExpectResult)
                    {
                        response.HasArgument = executionResponse.HasResult;
                        response.Argument = executionResponse.Result;
                    }

                    _ = SendNeonMessage(response);
                }
            }
            catch (RemotingException rex)
            {
                ProcessLocalExecutionExceptionInternal(request, rex);
            }
            catch (Exception ex)
            {
                Exception innermost = ex.GetInnermostException();
                RemotingException remotingException = new RemotingException(innermost);
                ProcessLocalExecutionExceptionInternal(request, remotingException);
            }
            finally
            {
                Interlocked.Decrement(ref executionQueueSize);
            }
        }
        
        void ProcessRemoteExecutionExceptionInternal(RemotingRequest remotingRequest, RemotingException exception)
        {
            logger.Error($"{LogsSign} remote execution exception on {remotingRequest}: {exception}");

            try
            {
                OnRemoteExecutionException(new RemoteExecutionExceptionEventArgs(new ExecutionRequest(remotingRequest), exception));
            }
            catch (Exception ex)
            {
                logger.Error($"{LogsSign} got an unhandled exception in OnRemoteExecutionException(): {ex}");
            }
        }
        
        void ProcessLocalExecutionExceptionInternal(RemotingRequest remotingRequest, RemotingException exception)
        {
            logger.Error($"{LogsSign} local method execution exception on {remotingRequest}: {exception}");

            if (remotingRequest.ExpectResponse)
            {
                RemotingResponseError remotingResponseError = new RemotingResponseError(
                    remotingRequest.RequestId,
                    remotingRequest.MethodKey,
                    exception);


                _ = SendNeonMessage(remotingResponseError);
            }

            try
            {
                LocalExecutionExceptionEventArgs eventArgs = new LocalExecutionExceptionEventArgs(exception,
                    new ExecutionRequest(remotingRequest));
                OnLocalExecutionException(eventArgs);
            }
            catch (Exception ex)
            {
                logger.Error($"{LogsSign} got an unhandled exception in {nameof(OnLocalExecutionException)}(): {ex}");
            }
        }
        
        protected virtual void OnLocalExecutionStarting(LocalExecutionStartingEventArgs args)
        {

        }
        
        protected virtual void OnLocalExecutionCompleted(LocalExecutionCompletedEventArgs args)
        {

        }

        protected virtual void OnLocalExecutionException(LocalExecutionExceptionEventArgs args)
        {

        }

        protected virtual void OnRemoteExecutionStarting(RemoteExecutionStartingEventArgs args)
        {

        }
        
        protected virtual void OnRemoteExecutionCompleted(RemoteExecutionCompletedEventArgs args)
        {

        }
        
        protected virtual void OnRemoteExecutionException(RemoteExecutionExceptionEventArgs args)
        {

        }

        protected abstract Task<ExecutionResponse> ExecuteRequestAsync(ExecutionRequest request);

        RemotingRequest GetRequest(object methodIdentity, bool expectResult, bool expectResponse)
        {
            if (!(methodIdentity is int || methodIdentity is string))
                throw new ArgumentException($"{nameof(methodIdentity)} should be int or string");
            if (!expectResponse && expectResult)
                throw new ArgumentException($"You can't reset {nameof(expectResponse)} and set {nameof(expectResult)}",
                    nameof(expectResponse));


            CheckConnected();
            RemotingRequest request = new RemotingRequest();
            request.MethodKey = methodIdentity;
            request.HasArgument = false;
            request.Argument = null;
            request.ExpectResult = expectResult;
            request.ExpectResponse = expectResponse;

            lock (lastRequestIdMutex)
                request.RequestId = lastRequestId++;

            lock (requests)
            {
                if (expectResponse)
                    requests.Add(request.RequestId, request);
            }

            if (expectResult)
                request.CreateAwaiter();

            return request;
        }
        
        protected virtual Task RemoteExecutionWrapper(ExecutionOptions options, Task executionTask)
        {
            return executionTask;
        }
        
        async Task SendAndWait(RemotingRequest request, ExecutionOptions options)
        {
            try
            {
                this.logger.Debug($"{LogsSign} executing {request} remotely with {options}");
                ExecutionRequest executionRequest = new ExecutionRequest(request);
                await SendNeonMessage(request).ConfigureAwait(false);
                options.CancellationToken.ThrowIfCancellationRequested();
                OnRemoteExecutionStarting(new RemoteExecutionStartingEventArgs(executionRequest, options));
                int timeout = defaultExecutionTimeout;
                if (options.Timeout > Timeout.Infinite)
                    timeout = options.Timeout;
                await RemoteExecutionWrapper(options, request.WaitAsync(timeout, options.CancellationToken)).ConfigureAwait(false);
                float ms = request.Response.ExecutionTime / (float) TimeSpan.TicksPerMillisecond;
                this.logger.Info($"{LogsSign} executed {request} remotely in {ms.ToString("0.00")}ms ({request.RemoteExecutionTime.TotalMilliseconds.ToString("0.00")}ms)");
                OnRemoteExecutionCompleted(new RemoteExecutionCompletedEventArgs(executionRequest,
                    new ExecutionResponse(request.Response), options, ms));
            }
            catch (Exception ex)
            {
                Exception inner = ex.GetInnermostException();
                RemotingException remotingException = inner as RemotingException;
                if (remotingException == null)
                    remotingException = new RemotingException(inner);
                
                throw remotingException;
            }
        }
        
        /// <summary>
        /// Executes remoting method identified by integer with no result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual Task ExecuteAsync(int methodIdentity) => ExecuteAsyncInternal(methodIdentity, ExecutionOptions.Default);
        
        /// <summary>
        /// Executes remoting method identified by string name with no result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual Task ExecuteAsync(string methodIdentity) => ExecuteAsyncInternal(methodIdentity, ExecutionOptions.Default);
        
        /// <summary>
        /// Executes remoting method identified by integer with no result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual Task ExecuteAsync(int methodIdentity, ExecutionOptions options) => ExecuteAsyncInternal(methodIdentity, options);
        
        /// <summary>
        /// Executes remoting method identified by string name with no result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual Task ExecuteAsync(string methodIdentity, ExecutionOptions options) => ExecuteAsyncInternal(methodIdentity, options);

        Task ExecuteAsyncInternal(object methodIdentity, ExecutionOptions options)
        {
            CheckConnected();
            RemotingRequest request = GetRequest(methodIdentity, true, true);
            request.HasArgument = false;
            return SendAndWait(request, options);
        }

        /// <summary>
        /// Executes remoting method identified by integer with argument passed and no result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual Task ExecuteAsync<A>(int methodIdentity, A arg) => ExecuteAsyncInternal(methodIdentity, arg, ExecutionOptions.Default);
        
        /// <summary>
        /// Executes remoting method identified by string name with argument passed and no result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual Task ExecuteAsync<A>(string methodIdentity, A arg) => ExecuteAsyncInternal(methodIdentity, arg, ExecutionOptions.Default);
        
        /// <summary>
        /// Executes remoting method identified by integer with argument passed and no result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual Task ExecuteAsync<A>(int methodIdentity, A arg, ExecutionOptions options) => ExecuteAsyncInternal(methodIdentity, arg, options);
        
        /// <summary>
        /// Executes remoting method identified by string name with argument passed and no result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual Task ExecuteAsync<A>(string methodIdentity, A arg, ExecutionOptions options) => ExecuteAsyncInternal(methodIdentity, arg, options);

        Task ExecuteAsyncInternal<A>(object methodIdentity, A arg, ExecutionOptions options)
        {
            CheckConnected();
            RemotingRequest request = GetRequest(methodIdentity, true, true);
            request.HasArgument = true;
            request.Argument = arg;
            return SendAndWait(request, options);
        }

        /// <summary>
        /// Executes remoting method identified by integer with result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual Task<R> ExecuteAsync<R>(int methodIdentity) => ExecuteAsyncInternal<R>(methodIdentity, ExecutionOptions.Default);
        
        /// <summary>
        /// Executes remoting method identified by string name with result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual Task<R> ExecuteAsync<R>(string methodIdentity) => ExecuteAsyncInternal<R>(methodIdentity, ExecutionOptions.Default);
        
        /// <summary>
        /// Executes remoting method identified by integer with result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual Task<R> ExecuteAsync<R>(int methodIdentity, ExecutionOptions options) => ExecuteAsyncInternal<R>(methodIdentity, options);
        
        /// <summary>
        /// Executes remoting method identified by string name with result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual Task<R> ExecuteAsync<R>(string methodIdentity, ExecutionOptions options) => ExecuteAsyncInternal<R>(methodIdentity, options);

        async Task<R> ExecuteAsyncInternal<R>(object methodIdentity, ExecutionOptions options)
        {
            CheckConnected();
            RemotingRequest request = GetRequest(methodIdentity, true, true);
            request.HasArgument = false;
            await SendAndWait(request, options).ConfigureAwait(false);
            return (R)request.Result;
        }

        /// <summary>
        /// Executes remoting method identified by integer with argument passed and result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual Task<R> ExecuteAsync<R, A>(int methodIdentity, A arg) => ExecuteAsyncInternal<R, A>(methodIdentity, arg, ExecutionOptions.Default);
        
        /// <summary>
        /// Executes remoting method identified by string name with argument passed and result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual Task<R> ExecuteAsync<R, A>(string methodIdentity, A arg) => ExecuteAsyncInternal<R, A>(methodIdentity, arg, ExecutionOptions.Default);
        
        /// <summary>
        /// Executes remoting method identified by integer with argument passed and result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual Task<R> ExecuteAsync<R, A>(int methodIdentity, A arg, ExecutionOptions options) => ExecuteAsyncInternal<R, A>(methodIdentity, arg, options);
        
        /// <summary>
        /// Executes remoting method identified by string name with argument passed and result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual Task<R> ExecuteAsync<R, A>(string methodIdentity, A arg, ExecutionOptions options) => ExecuteAsyncInternal<R, A>(methodIdentity, arg, options);

        async Task<R> ExecuteAsyncInternal<R, A>(object methodIdentity, A arg, ExecutionOptions options)
        {
            CheckConnected();
            RemotingRequest request = GetRequest(methodIdentity, true, true);
            request.HasArgument = true;
            request.Argument = arg;
            await SendAndWait(request, options).ConfigureAwait(false);
            return (R)request.Result;
        }

        /// <summary>
        /// Executes remoting method identified by integer with no completion waiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        public virtual Task Send(int methodIdentity)
        {
            return SendInternal(methodIdentity, SendingOptions.Default);
        }

        /// <summary>
        /// Executes remoting method identified by string name with no completion waiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        public virtual Task Send(string methodIdentity)
        {
            return SendInternal(methodIdentity, SendingOptions.Default);
        }

        /// <summary>
        /// Executes remoting method identified by integer with no completion waiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="options">Sending options</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        public virtual Task Send(int methodIdentity, SendingOptions options)
        {
            return SendInternal(methodIdentity, options);
        }

        /// <summary>
        /// Executes remoting method identified by string name with no completion waiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="options">Sending options</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        public virtual Task Send(string methodIdentity, SendingOptions options)
        {
            return SendInternal(methodIdentity, options);
        }

        async Task SendInternal(object methodIdentity, SendingOptions sendingOptions)
        {
            try
            {
                CheckConnected();
                RemotingRequest request = GetRequest(methodIdentity, false, sendingOptions.ExpectResponse);
                request.HasArgument = false;
                await SendInternalFinal(request, sendingOptions).ConfigureAwait(false);

                this.logger.Info($"{LogsSign} sent {request} remotely with no waiting");
            }
            catch (Exception ex)
            {
                Exception inner = ex.GetInnermostException();
                RemotingException remotingException = inner as RemotingException;
                if (remotingException == null)
                    remotingException = new RemotingException(inner);

                throw remotingException;
            }
        }

        /// <summary>
        /// Executes remoting method identified by integer with argument, but no completion waiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        public virtual Task Send<T>(int methodIdentity, T arg)
        {
            return SendInternal(methodIdentity, arg, SendingOptions.Default);
        }

        /// <summary>
        /// Executes remoting method identified by string name with argument, but no completion waiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        public virtual Task Send<T>(string methodIdentity, T arg)
        {
            return SendInternal(methodIdentity, arg, SendingOptions.Default);
        }

        /// <summary>
        /// Executes remoting method identified by integer with argument, but no completion waiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Sending options</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        public virtual Task Send<T>(int methodIdentity, T arg, SendingOptions options)
        {
            return SendInternal(methodIdentity, arg, options);
        }

        /// <summary>
        /// Executes remoting method identified by string name with argument, but no completion waiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Sending options</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        public virtual Task Send<T>(string methodIdentity, T arg, SendingOptions options)
        {
            return SendInternal(methodIdentity, arg, options);
        }

        async Task SendInternal<T>(object methodIdentity, T arg, SendingOptions sendingOptions)
        {
            try
            {
                CheckConnected();
                RemotingRequest request = GetRequest(methodIdentity, false, sendingOptions.ExpectResponse);
                request.HasArgument = true;
                request.Argument = arg;
                await SendInternalFinal(request, sendingOptions).ConfigureAwait(false);

                this.logger.Info($"{LogsSign} sent {request} remotely with no waiting");
            }
            catch (Exception ex)
            {
                Exception inner = ex.GetInnermostException();
                RemotingException remotingException = inner as RemotingException;
                if (remotingException == null)
                    remotingException = new RemotingException(inner);

                throw remotingException;
            }
        }

        private protected virtual Task SendInternalFinal(INeonMessage message, SendingOptions option)
        {
            return SendNeonMessage(message, option.DeliveryType, option.Channel);
        }
    }
}