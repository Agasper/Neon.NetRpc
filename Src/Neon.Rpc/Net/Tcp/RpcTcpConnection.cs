using System;
using System.Threading;
using System.Threading.Tasks;
using Middleware;
using Neon.Logging;
using Neon.Networking;
using Neon.Rpc.Net.Events;
using Neon.Networking.Messages;
using Neon.Networking.Tcp;
using Neon.Networking.Tcp.Events;
using Neon.Networking.Udp.Messages;
using Neon.Rpc.Authorization;
using Neon.Rpc.Serialization;
using ConnectionClosedEventArgs = Neon.Networking.Tcp.Events.ConnectionClosedEventArgs;
using Middlewares = Middleware.Middlewares;

namespace Neon.Rpc.Net.Tcp
{
    public class RpcTcpConnection : TcpConnection, IRpcConnectionInternal, IMiddlewareConnection
    {
        /// <summary>
        /// Current session bound (can be null)
        /// </summary>
        public RpcSession Session => session;
        /// <summary>
        /// Connection statistics
        /// </summary>
        IConnectionStatistics IRpcConnection.Statistics => this.Statistics;

        RpcSession session;
        AuthSessionBase authSession;
        object sessionMutex = new object();
        readonly RpcConfiguration configuration;
        readonly IRpcPeer rpcPeer;
        readonly Middlewares middlewares;
        new readonly ILogger logger;
        
        EncryptionMiddleware encryptionMiddleware;

        internal RpcTcpConnection(TcpPeer parent, IRpcPeer rpcPeer, RpcConfiguration configuration) : base(parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (rpcPeer == null)
                throw new ArgumentNullException(nameof(rpcPeer));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            this.configuration = configuration;
            this.rpcPeer = rpcPeer;
            this.middlewares = new Middlewares();
            this.logger = configuration.LogManager.GetLogger(nameof(RpcTcpConnection));
        }
        
        /// <summary>
        /// Returns the memory used by this connection
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.encryptionMiddleware?.Dispose();
        }

        internal async Task Start(CancellationToken cancellationToken)
        {
            this.middlewares.Add(new CompressionMiddleware(configuration.CompressionThreshold, this,
                configuration.LogManager));
            if (configuration.IsCipherSet)
            {
                this.encryptionMiddleware = new EncryptionMiddleware(this, configuration.LogManager);
                this.encryptionMiddleware.Set(configuration.CreateNewCipher());
                this.middlewares.Add(this.encryptionMiddleware);
            }

            logger.Trace($"#{Id} starting middlewares...");
            using (CancellationTokenSource mergerCts =
                   CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, base.CancellationToken))
            {
                await this.middlewares.Start(mergerCts.Token).ConfigureAwait(false);
            }
            
            logger.Trace($"#{Id} middlewares started!");
        }

        internal void StartServerSession()
        {
            if (!(configuration is RpcTcpConfigurationServer serverConf))
                throw new InvalidOperationException(
                    $"Got wrong configuration {configuration.GetType().Name}, expected {nameof(RpcTcpConfigurationServer)}");

            if (serverConf.AuthSessionFactory != null)
            {
                logger.Debug($"#{Id} waiting for auth...");
                AuthSessionContext context =
                    new AuthSessionContext(configuration.TaskScheduler, configuration.LogManager, this, InitSessionSafe);

                this.authSession = serverConf.AuthSessionFactory.CreateSession(context);
            }
            else
            {
                InitSessionSafe(null);
            }
        }

        internal async Task StartClientSession(bool authRequired, object authObject, CancellationToken cancellationToken)
        {
            using (CancellationTokenSource mergedTcs =
                   CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, base.CancellationToken))
            {
                CheckToken(mergedTcs.Token);

                if (authRequired)
                {
                    try
                    {
                        logger.Debug($"#{Id} waiting for auth...");
                        AuthSessionContext context =
                            new AuthSessionContext(configuration.TaskScheduler, configuration.LogManager, this, null);

                        var authSession_ = new AuthSessionClient(context);
                        this.authSession = authSession_;
                        await authSession_.Start(authObject, mergedTcs.Token).ConfigureAwait(false);
                        this.authSession = null;

                        CheckToken(mergedTcs.Token);
                        InitSessionSafe(authSession_);
                    }
                    finally
                    {
                        this.authSession = null;
                    }
                }
                else
                    InitSessionSafe(null);

            }
        }

        void CheckToken(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                throw new OperationCanceledException("Connection was closed prematurely");
        }

        /// <summary>
        /// Creating a new empty message with a length defaulted by memory manager 
        /// </summary>
        /// <returns>A new message</returns>
        public RawMessage CreateMessage()
        {
            return Parent.CreateMessage();
        }

        /// <summary>
        /// Creating a new empty message with a predefined size. Returning message size may be bigger than requested value
        /// </summary>
        /// <param name="length">Size in bytes</param>
        /// <returns>A new message</returns>
        public RawMessage CreateMessage(int length)
        {
            return Parent.CreateMessage(length);
        }
        
        /// <summary>
        /// Creating a new empty RPC message with a length defaulted by memory manager 
        /// </summary>
        /// <returns>A new RPC message</returns>
        public RpcMessage CreateRpcMessage()
        {
            return new RpcMessage(configuration.Serializer, Parent.CreateMessage());
        }
        
        /// <summary>
        /// Creating a new empty RPC message with a predefined size. Returning message size may be bigger than requested value
        /// </summary>
        /// <param name="length">Size in bytes</param>
        /// <returns>A new RPC message</returns>
        public RpcMessage CreateRpcMessage(int length)
        {
            return new RpcMessage(configuration.Serializer, Parent.CreateMessage(length));
        }
        
        void InitSessionSafe(AuthSessionBase authSession)
        {
            try
            {
                InitSession(authSession);
            }
            catch (Exception e)
            {
                logger.Error($"#{Id} failed to create session: {e}");
                this.Close();
            }
        }

        void InitSession(AuthSessionBase authSession)
        {
            if (this.session != null)
                throw new InvalidOperationException("Session already created!");

            this.authSession = null;
                
            lock (sessionMutex)
            {
                if (!this.Connected)
                    return;

                RpcSessionContext context = new RpcSessionContext(configuration.DefaultExecutionTimeout,
                    configuration.OrderedExecution, configuration.OrderedExecutionMaxQueue, configuration.TaskScheduler,
                    configuration.LogManager, this, configuration.RemotingInvocationRules, authSession);

                var session_ = configuration.SessionFactory.CreateSession(context);

                this.session = session_;
                this.rpcPeer.OnSessionOpened(new SessionOpenedEventArgs(session_, this));
            }

            // return session_;
        }

        protected override void OnConnectionClosed(ConnectionClosedEventArgs args)
        {
            base.OnConnectionClosed(args);

            this.authSession = null;

            lock (sessionMutex)
            {
                var session_ = this.session;
                if (session_ != null)
                {
                    session_.Close();
                    this.session = null;
                    this.rpcPeer.OnSessionClosed(new SessionClosedEventArgs(session_, this));
                }
            }
        }

        protected override void OnMessageReceived(MessageEventArgs args)
        {
            if (args == null) 
                throw new ArgumentNullException(nameof(args));
            
            logger.Trace($"#{Id} received {args.Message}");
            try
            {
                using (RawMessage message = this.middlewares.ProcessRecvMessage(args.Message))
                {
                    var authSession_ = this.authSession;
                    var session_ = this.session;
                    if (message != null)
                    {
                        if (authSession_ != null)
                            authSession_.OnMessage(new RpcMessage(configuration.Serializer, message));
                        else if (session_ != null)
                            session_.OnMessage(new RpcMessage(configuration.Serializer, message));
                        else
                            throw new InvalidOperationException("No session to handle message");
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error($"#{Id} got an unhandled exception in {nameof(RpcTcpConnection)}.{nameof(OnMessageReceived)}: {e}");
                Close();
            }
            finally
            {
                args?.Message?.Dispose();
            }
        }

        async Task SendMessageWithMiddlewaresAsync(RawMessage message, DeliveryType deliveryType, int channel)
        {
            if (message == null) 
                throw new ArgumentNullException(nameof(message));
            if (!this.Connected)
                throw new InvalidOperationException($"Connection is not established");
            
            try
            {
                using (RawMessage newMessage = this.middlewares.ProcessSendMessage(message))
                    await base.SendMessageAsync(newMessage).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Error($"#{Id} got an unhandled exception in {nameof(RpcTcpConnection)}.SendMessageWithMiddlewaresAsync: {e}");
                Close();
            }
            finally
            {
                message?.Dispose();
            }
        }

        Task IMiddlewareConnection.SendMessageWithMiddlewaresAsync(RawMessage message, DeliveryType deliveryType, int channel)
        {
            return SendMessageWithMiddlewaresAsync(message, deliveryType, channel);
        }
        
        public Task SendMessage(RpcMessage message, DeliveryType deliveryType, int channel)
        {
            return SendMessageWithMiddlewaresAsync((RawMessage)message, deliveryType, channel);
        }
    }
}
