using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Neon.Logging;
using Neon.Rpc.Messages;

namespace Neon.Rpc
{
    public class RpcSessionBase : IRpcSession
    {
        /// <summary>
        /// Underlying transport connection
        /// </summary>
        public IRpcConnection Connection => _context.Connection;
        
        RpcSessionContext _context;
        bool _closed;
        readonly ILogger _logger;
        
        public RpcSessionBase(RpcSessionContext context)
        {
            _context = context;
            _logger = context.LogManager.GetLogger(typeof(RpcSessionBase));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IMessage CheckNull(IMessage arg)
        {
            if (arg == null)
                throw new ArgumentNullException(nameof(arg));
            return arg;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        string CheckNull(string method)
        {
            if (string.IsNullOrEmpty(method))
                throw new ArgumentNullException(nameof(method));
            return method;
        }

        internal void CheckClosedInternal()
        {
            if (_closed)
                throw new RpcException("Remote session is closed", RpcResponseStatusCode.InvalidOperation);
        }
        
        void CheckClosed()
        {
            if (_closed)
                throw new RpcException("Session is closed", RpcResponseStatusCode.InvalidOperation);
        }

        public async Task CloseGracefully(CancellationToken cancellationToken)
        {
            if (_closed)
                return;
            _closed = true;
            await Task.Factory.StartNew(() => OnClose(CancellationToken.None),
                CancellationToken.None, TaskCreationOptions.None,
                _context.TaskScheduler ?? TaskScheduler.Default).Unwrap();
            _context.Connection.Close();
        }

        protected internal virtual Task OnInit(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected internal virtual Task OnClose(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected internal virtual Task<IMessage> LocalExecutionInterceptor(Task<IMessage> task, string method, IMessage arg, CancellationToken cancellationToken)
        {
            return task;
        }
        
        protected virtual Task<R> RemoteExecutionInterceptor<R>(Task<R> task, string method, IMessage arg, ExecutionOptions options) where R : IMessage<R>, new()
        {
            return task;
        }
        
        async Task<R> RemoteExecutionInterceptorInternal<R>(Task<R> task, string method, IMessage arg, ExecutionOptions options) where R : IMessage<R>, new() 
        {
            CheckClosed();
            return await RemoteExecutionInterceptor(task, method, arg, options);
        }

        /// <summary>
        /// Executes remoting method identified by string name with no result awaiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual async Task ExecuteAsync(string method) =>
            await RemoteExecutionInterceptorInternal(
                _context.Controller.ExecuteUserMethodInternalAsync<Empty>(CheckNull(method), null, ExecutionOptions.Default),
                CheckNull(method), null, ExecutionOptions.Default).ConfigureAwait(false);

        /// <summary>
        /// Executes remoting method identified by string name with no result awaiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual async Task ExecuteAsync(string method, ExecutionOptions options) =>
            await RemoteExecutionInterceptorInternal(
                _context.Controller.ExecuteUserMethodInternalAsync<Empty>(CheckNull(method), null, options), CheckNull(method), null,
                ExecutionOptions.Default).ConfigureAwait(false);


        /// <summary>
        /// Executes remoting method identified by string name with argument passed and no result awaiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual async Task ExecuteAsync<A>(string method, A arg) where A : IMessage =>
            await RemoteExecutionInterceptorInternal(
                _context.Controller.ExecuteUserMethodInternalAsync<Empty>(CheckNull(method), CheckNull(arg),
                    ExecutionOptions.Default), CheckNull(method), CheckNull(arg), ExecutionOptions.Default).ConfigureAwait(false);

        /// <summary>
        /// Executes remoting method identified by string name with argument passed and no result awaiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual async Task ExecuteAsync<A>(string method, A arg, ExecutionOptions options) where A : IMessage =>
            await RemoteExecutionInterceptorInternal(
                _context.Controller.ExecuteUserMethodInternalAsync<Empty>(CheckNull(method), CheckNull(arg), options), CheckNull(method),
                CheckNull(arg), ExecutionOptions.Default).ConfigureAwait(false);


        /// <summary>
        /// Executes remoting method identified by string name with result awaiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual async Task<R> ExecuteAsync<R>(string method) where R : IMessage<R>, new() =>
            await RemoteExecutionInterceptorInternal(
                _context.Controller.ExecuteUserMethodInternalAsync<R>(CheckNull(method), null, ExecutionOptions.Default), CheckNull(method),
                null, ExecutionOptions.Default).ConfigureAwait(false);

        /// <summary>
        /// Executes remoting method identified by string name with result awaiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual async Task<R> ExecuteAsync<R>(string method, ExecutionOptions options)
            where R : IMessage<R>, new() =>
            await RemoteExecutionInterceptorInternal(
                _context.Controller.ExecuteUserMethodInternalAsync<R>(CheckNull(method), null, options), CheckNull(method), null,
                ExecutionOptions.Default).ConfigureAwait(false);

        /// <summary>
        /// Executes remoting method identified by string name with argument passed and result awaiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual async Task<R> ExecuteAsync<R, A>(string method, A arg)
            where R : IMessage<R>, new() where A : IMessage => await RemoteExecutionInterceptorInternal(
            _context.Controller.ExecuteUserMethodInternalAsync<R>(CheckNull(method), CheckNull(arg), ExecutionOptions.Default),
            CheckNull(method), CheckNull(arg), ExecutionOptions.Default).ConfigureAwait(false);

        /// <summary>
        /// Executes remoting method identified by string name with argument passed and result awaiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        public virtual async Task<R> ExecuteAsync<R, A>(string method, A arg, ExecutionOptions options)
            where R : IMessage<R>, new() where A : IMessage =>
            await RemoteExecutionInterceptorInternal(
                _context.Controller.ExecuteUserMethodInternalAsync<R>(CheckNull(method), CheckNull(arg), options), CheckNull(method), CheckNull(arg),
                ExecutionOptions.Default).ConfigureAwait(false);


        /// <summary>
        /// Executes remoting method identified by string name with no completion waiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        public virtual async Task Send(string method)
        {
            CheckClosed();
            await _context.Controller.SendUserMethodInternalAsync(CheckNull(method), null, SendingOptions.Default).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes remoting method identified by string name with no completion waiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="options">Sending options</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        public virtual async Task Send(string method, SendingOptions options)
        {
            CheckClosed();
            await _context.Controller.SendUserMethodInternalAsync(CheckNull(method), null, options).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes remoting method identified by string name with argument, but no completion waiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        public virtual async Task Send<T>(string method, T arg) where T : IMessage
        {
            CheckClosed();
            await _context.Controller.SendUserMethodInternalAsync(CheckNull(method), CheckNull(arg), SendingOptions.Default).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes remoting method identified by string name with argument, but no completion waiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Sending options</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        public virtual async Task Send<T>(string method, T arg, SendingOptions options) where T : IMessage
        {
            CheckClosed();
            await _context.Controller.SendUserMethodInternalAsync(CheckNull(method), CheckNull(arg), options).ConfigureAwait(false);
        }
    }
}