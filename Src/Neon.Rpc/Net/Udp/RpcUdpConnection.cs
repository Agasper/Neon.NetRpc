using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Middleware;
using Neon.Networking;
using Neon.Rpc.Net.Events;
using Neon.Networking.Messages;
using Neon.Networking.Udp;
using Neon.Networking.Udp.Messages;
using Neon.Rpc.Authorization;
using Neon.Rpc.Serialization;
using ConnectionClosedEventArgs = Neon.Networking.Udp.Events.ConnectionClosedEventArgs;
using ConnectionOpenedEventArgs = Neon.Networking.Udp.Events.ConnectionOpenedEventArgs;

namespace Neon.Rpc.Net.Udp
{
    public class RpcUdpConnection : UdpConnection, IRpcConnectionInternal, IMiddlewareConnection
    {
        public RpcSession Session => session;
        IConnectionStatistics IRpcConnection.Statistics => this.Statistics;
        public EndPoint RemoteEndpoint => base.EndPoint.EndPoint;
        
        RpcSession session;
        AuthSessionBase authSession;
        readonly object sessionMutex = new object();
        readonly RpcConfiguration configuration;
        readonly IRpcPeer rpcPeer;
        readonly Middlewares middlewares;
        EncryptionMiddleware encryptionMiddleware;

        internal RpcUdpConnection(UdpPeer parent, IRpcPeer rpcPeer, RpcConfiguration configuration) : base(parent)
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
        }
        
        public override void Dispose()
        {
            base.Dispose();
            this.encryptionMiddleware?.Dispose();
        }
        
        public async Task Start(CancellationToken cancellationToken)
        {
            this.middlewares.Add(new CompressionMiddleware(configuration.CompressionThreshold, this,
                configuration.LogManager));
            if (configuration.IsCipherSet)
            {
                this.encryptionMiddleware = new EncryptionMiddleware(this, configuration.LogManager);
                this.encryptionMiddleware.Set(configuration.CreateNewCipher());
                this.middlewares.Add(this.encryptionMiddleware);
            }

            logger.Debug($"#{Id} starting middlewares...");
            using (CancellationTokenSource mergerCts =
                   CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, base.CancellationToken))
            {
                await this.middlewares.Start(mergerCts.Token).ConfigureAwait(false);
            }
            
            logger.Trace($"#{Id} middlewares started!");
        }

        public void StartServerSession()
        {
            if (!(configuration is RpcUdpConfigurationServer serverConf))
                throw new InvalidOperationException(
                    $"Got wrong configuration {configuration.GetType().Name}, expected {nameof(RpcUdpConfigurationServer)}");

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

        public async Task StartClientSession(bool authRequired, object authObject, CancellationToken cancellationToken)
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
        }

        public RpcMessage CreateRpcMessage()
        {
            return new RpcMessage(configuration.Serializer, Parent.CreateMessage());
        }
        
        public RpcMessage CreateRpcMessage(int length)
        {
            return new RpcMessage(configuration.Serializer, Parent.CreateMessage(length));
        }

        public RawMessage CreateMessage()
        {
            return Parent.CreateMessage();
        }

        public RawMessage CreateMessage(int length)
        {
            return Parent.CreateMessage(length);
        }


        public Task SendMessageWithMiddlewaresAsync(RawMessage rawMessage)
        {
            throw new NotImplementedException();
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

        protected override void OnMessageReceived(UdpRawMessage udpRawMessage)
        {
            if (udpRawMessage == null) 
                throw new ArgumentNullException(nameof(udpRawMessage));
            
            try
            {
                using (RawMessage message = this.middlewares.ProcessRecvMessage(udpRawMessage.Message))
                {
                    var authSession_ = this.authSession;
                    var session_ = this.session;
                    if (message != null)
                    {
                        if (authSession_ != null)
                            authSession_.OnMessage(new RpcMessage(configuration.Serializer, message));
                        else if (session_ != null)
                            session_.OnMessage(new RpcMessage(configuration.Serializer, message));
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error($"#{Id} got an unhandled exception in {nameof(RpcUdpConnection)}.{nameof(OnMessageReceived)}: {e}");
                Close();
            }
            finally
            {
                udpRawMessage.Dispose();
            }
        }
        
        async Task SendMessageWithMiddlewaresAsync(RawMessage message, DeliveryType deliveryType, int channel)
        {
            if (message == null) 
                throw new ArgumentNullException(nameof(message));
            
            try
            {
                using (RawMessage newMessage = this.middlewares.ProcessSendMessage(message))
                {
                    if (newMessage != null)
                        await base.SendMessageAsync(new UdpRawMessage(newMessage, deliveryType, channel))
                            .ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                logger.Error($"#{Id} got an unhandled exception in {nameof(RpcUdpConnection)}.{nameof(SendMessageWithMiddlewaresAsync)}: {e}");
                Close();
            }
            finally
            {
                message?.Dispose();
            }
        }

        public void Close()
        {
            _ = this.CloseAsync();
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
