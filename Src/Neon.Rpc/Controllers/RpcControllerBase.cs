using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Neon.Logging;
using Neon.Networking.Cryptography;
using Neon.Networking.Messages;
using Neon.Networking.Udp;
using Neon.Networking.Udp.Messages;
using Neon.Rpc.Cryptography.Ciphers;
using Neon.Rpc.Messages;
using Neon.Rpc.Messages.Proto;

namespace Neon.Rpc.Controllers
{
    abstract class RpcControllerBase : IDisposable
    {
        /// <summary>
        /// Underlying transport connection
        /// </summary>
        public IRpcConnection Connection => _context.Connection;
        
        protected readonly RpcControllerContext _context;
        protected readonly ILogger _logger;
        
        readonly Dictionary<int, IPendingRpcRequest> _requests;
        readonly object _orderedExecutionTaskMutex = new object();
        
        private protected IRpcCipher _cipher;
        
        Task _orderedExecutionTask;
        int _lastRequestId;
        volatile int _executionQueueSize;
        bool _disposed;
        
        public RpcControllerBase(RpcControllerContext controllerContext)
        {
            if (controllerContext == null) 
                throw new ArgumentNullException(nameof(controllerContext));
            if (controllerContext.Connection == null)
                throw new ArgumentNullException(nameof(controllerContext.Connection));
            if (controllerContext.Configuration.LogManager == null)
                throw new ArgumentNullException(nameof(controllerContext.Configuration.LogManager));
            if (controllerContext.Configuration.MemoryManager == null)
                throw new ArgumentNullException(nameof(controllerContext.Configuration.MemoryManager));
            if (controllerContext.Configuration.TaskScheduler == null)
                throw new ArgumentNullException(nameof(controllerContext.Configuration.TaskScheduler));
            
            
            _requests = new Dictionary<int, IPendingRpcRequest>();
            _orderedExecutionTask = Task.CompletedTask;
            _context = controllerContext;
            _logger = controllerContext.Configuration.LogManager.GetLogger(typeof(RpcControllerBase));
            _logger.Meta["connection_id"] = controllerContext.Connection.Id;
            _logger.Meta["connection_endpoint"] = new RefLogLabel<IRpcConnection>(controllerContext.Connection, s => s.RemoteEndpoint);
            _logger.Meta["latency"] = new RefLogLabel<RpcControllerBase>(this, s =>
            {
                var lat = s._context.Connection.Statistics.Latency;
                if (lat.HasValue)
                    return lat.Value;
                return "";
            });
            
            _logger.Debug($"{GetLogsSign()} {this} created!");
        }

        internal void OnMessage(IRawMessage rawMessage)
        {
            CheckDisposed();
            if (_cipher != null)
            {
                if (!rawMessage.Encrypted)
                    throw new InvalidOperationException(
                        "We got unencrypted message, but encryption is set");
                
                using (var decryptedMessage = rawMessage.Decrypt(_cipher))
                {
                    _logger.Trace($"{GetLogsSign()} decrypting message {rawMessage} to {decryptedMessage}");
                    OnMessageDecrypted(decryptedMessage);
                }
            }
            else
            {
                if (rawMessage.Encrypted)
                    throw new InvalidOperationException(
                        "We got encrypted message but cipher is not set!");
                
                OnMessageDecrypted(rawMessage);
            }
        }

        void OnMessageDecrypted(IRawMessage rawMessage)
        {
            if (rawMessage.Compressed)
            {
                using (var decompressedMessage = rawMessage.Decompress())
                {
                    _logger.Trace($"{GetLogsSign()} decompressing message {rawMessage} to {decompressedMessage}");
                    OnMessageClear(decompressedMessage);
                }
            }
            else
                OnMessageClear(rawMessage);
        }

        void OnMessageClear(IRawMessage rawMessage)
        {
            try
            {
                RpcMessageHeaderProto header = RpcMessageBase.ExtractHeader(rawMessage);
                RpcPayload payload = RpcMessageBase.ExtractPayload(_context.Configuration.MemoryManager, rawMessage, header);
                RpcMessageBase baseMessage = null;

                try
                {
                    switch (header.Data.MessageTypeCase)
                    {
                        case RpcMessageHeaderDataProto.MessageTypeOneofCase.RpcRequest:
                            RpcRequest rpcRequest = new RpcRequest(_context.Configuration.MemoryManager, header, payload);
                            baseMessage = rpcRequest;
                            if (!_logger.IsFiltered(LogSeverity.TRACE))
                                _logger.Trace($"{GetLogsSign()} received a message {rpcRequest.ToString(_context.Configuration.LogMessageBodyDebug)} from {rawMessage}");
                            OnRpcRequest(rpcRequest);
                            break;
                        case RpcMessageHeaderDataProto.MessageTypeOneofCase.RpcResponse:
                            RpcResponse rpcResponse = new RpcResponse(_context.Configuration.MemoryManager, header, payload);
                            baseMessage = rpcResponse;
                            if (!_logger.IsFiltered(LogSeverity.TRACE))
                                _logger.Trace($"{GetLogsSign()} received a message {rpcResponse.ToString(_context.Configuration.LogMessageBodyDebug)} from {rawMessage}");
                            OnRpcResponse(rpcResponse);
                            break;
                        default:
                            throw new InvalidOperationException($"Wrong message type {header.Data.MessageTypeCase}");
                    }
                }
                catch
                {
                    baseMessage?.Dispose();
                    payload.Dispose();
                    throw;
                }
            }
            catch (Exception outerException)
            {
                _logger.Error(
                    $"{GetLogsSign()} got an unhandled exception on {nameof(RpcControllerBase)}.{nameof(OnMessage)}(): {outerException}");
                _context.Connection.Close();
            }
        }

        async Task EnqueueAndSynchronizeRequest(RpcRequest request, CancellationToken cancellationToken)
        {
            if (_context.Configuration.OrderedExecution && _executionQueueSize >= _context.Configuration.OrderedExecutionMaxQueue)
            {
                RpcException ex = new RpcException("Queue is full", RpcResponseStatusCode.Exhausted);
                using(var response = GetErrorResponse(request.MessageId, ex))
                    await SendRpcMessageAsync(response, cancellationToken);
                throw ex;
            }

            if (!_context.Configuration.OrderedExecution)
            {
                await Task.Factory.StartNew(() => ExecuteRequestInternalAsync(request, cancellationToken), 
                    cancellationToken, TaskCreationOptions.None,
                    _context.Configuration.TaskScheduler ?? TaskScheduler.Default).Unwrap();
            }
            else
            {
                Task newTask;
                lock (_orderedExecutionTaskMutex)
                {
                    Interlocked.Increment(ref _executionQueueSize);
                    newTask = _orderedExecutionTask.ContinueWith((t, o)
                        => ExecuteRequestInternalAsync(o as RpcRequest, cancellationToken), request,
                        cancellationToken,
                            TaskContinuationOptions.ExecuteSynchronously,
                        _context.Configuration.TaskScheduler ?? TaskScheduler.Default).Unwrap();
                    _orderedExecutionTask = newTask;
                }
                await newTask;
            }
        }
        
        async Task ExecuteRequestInternalAsync(RpcRequest request, CancellationToken cancellationToken)
        {
            try
            {
                // Stopwatch sw = new Stopwatch();
                // sw.Start();
                var responsePayload =
                    await OnRpcRequestExecute(request, cancellationToken).ConfigureAwait(false);
                if (request.ExpectResponse)
                {
                    using (RpcResponse response = new RpcResponse(_context.Configuration.MemoryManager))
                    {
                        response.MessageId = request.MessageId;
                        if (responsePayload != null)
                            response.SetPayload(responsePayload);
                        // sw.Stop();
                        await SendRpcMessageAsync(response, cancellationToken).ConfigureAwait(false);
                    }
                }

                // double timeSeconds = sw.ElapsedTicks / (double) Stopwatch.Frequency;
                // ulong timeSpanTicks = (ulong) (timeSeconds * TimeSpan.TicksPerSecond);
                // float ms = timeSpanTicks / (float) TimeSpan.TicksPerMillisecond;
            }
            catch (Exception ex)
            {
                using(var response = GetErrorResponse(request.MessageId, RpcException.ConvertException(ex)))
                    await SendRpcMessageAsync(response, cancellationToken);
                throw;
            }
            finally
            {
                if (_context.Configuration.OrderedExecution)
                    Interlocked.Decrement(ref _executionQueueSize);
            }
        }

        void OnRpcRequest(RpcRequest request)
        {
            _logger.Trace($"{GetLogsSign()} starting local execution of {request}");
            EnqueueAndSynchronizeRequest(request, _context.Connection.CancellationToken).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    _logger.Error($"{GetLogsSign()} local exception in request processing {request}", t.Exception);
                else if (t.IsCanceled)
                    _logger.Debug($"{GetLogsSign()} local execution cancelled for request {request}");
                else if(t.IsCompleted)
                    _logger.Debug($"{GetLogsSign()} locally executed {request} successfully");
                request.Dispose();
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        void OnRpcResponse(RpcResponse rpcResponse)
        {
            using (rpcResponse)
            {
                IPendingRpcRequest pendingRpcRequest;

                lock (_requests)
                {
                    if (!_requests.TryGetValue(rpcResponse.MessageId, out pendingRpcRequest))
                    {
                        _logger.Debug($"{GetLogsSign()} got response for unknown request id {rpcResponse}");
                        return;
                    }
                }
                
                // _logger.Trace($"{GetLogsSign()} getting response {rpcResponse} for {pendingRpcRequest}");
                pendingRpcRequest.SetResult(rpcResponse);
            }
        }

        protected abstract Task<IMessage> OnRpcRequestExecute(RpcRequest request, CancellationToken cancellationToken);


        RpcResponse GetErrorResponse(int messageId, RpcException exception)
        {
            RpcResponse response = new RpcResponse(_context.Configuration.MemoryManager);
            response.MessageId = messageId;
            response.StatusCode = exception.StatusCode;
            response.SetPayload(exception.CreateProto());
            return response;
        }


        PendingRpcRequest<T> CreateRequest<T>(string method, bool expectResponse, IMessage arg) where T : IMessage<T>, new()
        {
            PendingRpcRequest<T> request = new PendingRpcRequest<T>(expectResponse);
            request.Method = method;
            request.Argument = arg;
            request.ExpectResponse = expectResponse;
            request.RequestId = Interlocked.Increment(ref _lastRequestId);

            if (expectResponse)
                lock (_requests)
                    _requests.Add(request.RequestId, request);

            return request;
        }

        protected async Task<T> ExecuteInternalAsync<T>(string method, IMessage arg, ExecutionOptions options)
            where T : IMessage<T>, new()
        {
            CheckDisposed();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            PendingRpcRequest<T> request = CreateRequest<T>(method, true, arg);
            try
            {
                using (var rpcRequest = request.CreateRpcRequest(_context.Configuration.MemoryManager))
                {
                    _logger.Trace($"{GetLogsSign()} remote request {request} execution starting");
                    await SendRpcMessageAsync(rpcRequest, options.CancellationToken).ConfigureAwait(false);
                }

                T result = await request.WaitAsync(options.CancellationToken).ConfigureAwait(false);
                double ms = stopwatch.ElapsedMilliseconds;
                _logger.Trace($"{GetLogsSign()} remote request {request} execution completed in {ms.ToString("0.00")} ms");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"{GetLogsSign()} remote request {request} execution failed", ex);
                throw;
            }
            finally
            {
                lock (_requests)
                    _requests.Remove(request.RequestId);
            }
        }
        
        protected async Task SendInternalAsync(string method, IMessage arg, SendingOptions sendingOptions)
        {
            CheckDisposed();
            PendingRpcRequest<Empty> request = CreateRequest<Empty>(method, false, arg);
            try
            {
                // _logger.Trace($"{GetLogsSign()} remote request {request} sending started");
                using (RpcRequest rpcRequest = request.CreateRpcRequest(_context.Configuration.MemoryManager))
                {
                    await SendRpcMessageAsync(rpcRequest, sendingOptions.CancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{GetLogsSign()} remote request {request} sending failed", ex);
                throw;
            }
        }
        
        public override string ToString()
        {
            return $"{GetType().Name}[connection_id={_context.Connection.Id},endpoint={_context.Connection.RemoteEndpoint}]";
        }

        Task SendRpcMessageAsync(RpcMessageBase rpcMessage, CancellationToken cancellationToken)
        {
            return SendRpcMessageAsync(rpcMessage, DeliveryType.ReliableOrdered, UdpConnection.DEFAULT_CHANNEL, cancellationToken);
        }

        async Task SendRpcMessageAsync(RpcMessageBase rpcMessage, DeliveryType deliveryType, int channel, CancellationToken cancellationToken)
        {
            CheckDisposed();
            using (var rawMessage = _context.Connection.CreateMessage(rpcMessage.CalculateSize()))
            {
                try
                {
                    rpcMessage.WriteTo(rawMessage);
                    
                    if (!_logger.IsFiltered(LogSeverity.TRACE))
                        _logger.Trace($"{GetLogsSign()} sending {rpcMessage.ToString(_context.Configuration.LogMessageBodyDebug)} as {rawMessage}");

                    await CompressAndSendRawMessageAsync(rawMessage, deliveryType, channel, cancellationToken);
                    _logger.Debug($"{GetLogsSign()} sent {rpcMessage}");
                }
                finally
                {
                    rawMessage.Dispose();
                }
            }
        }

        async Task CompressAndSendRawMessageAsync(IRawMessage rawMessage, DeliveryType deliveryType, int channel,
            CancellationToken cancellationToken)
        {
            if (rawMessage.Length >= _context.Configuration.CompressionThreshold)
            {
                using (var compressedMessage = rawMessage.Compress(CompressionLevel.Optimal))
                {
                    _logger.Trace($"{GetLogsSign()} compressing message {rawMessage} to {compressedMessage}");
                    await EncryptAndSendRawMessageAsync(compressedMessage, deliveryType, channel, cancellationToken);
                }
            }
            else
                await EncryptAndSendRawMessageAsync(rawMessage, deliveryType, channel, cancellationToken);
        }

        async Task EncryptAndSendRawMessageAsync(IRawMessage rawMessage, DeliveryType deliveryType, int channel,
            CancellationToken cancellationToken)
        {
            if (_cipher != null)
            {
                using (var encryptedMessage = rawMessage.Encrypt(_cipher))
                {
                    _logger.Trace($"{GetLogsSign()} encrypting message {rawMessage} to {encryptedMessage}");
                    await _context.Connection.SendMessage(encryptedMessage, deliveryType, channel, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            else
                await _context.Connection.SendMessage(rawMessage, deliveryType, channel, cancellationToken).ConfigureAwait(false);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(this.GetType().Name);
        }
        
        private protected virtual void OnDisposed()
        {
            _cipher?.Dispose();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            
            lock (_requests)
            {
                foreach (var pair in _requests)
                {
                    pair.Value.SetCancelled();
                }

                _requests.Clear();
            }
            
            _cipher?.Dispose();

            OnDisposed();
            
            _logger.Debug($"{GetLogsSign()} {this} disposed!");
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected string GetLogsSign()
        {
            return $"#{_context.Connection.Id}";
        }
    }
}