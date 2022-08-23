using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Neon.Rpc.Net.Events;
using Neon.Logging;
using Neon.Networking.Tcp;
using Neon.Networking.Tcp.Events;

namespace Neon.Rpc.Net.Tcp
{
    public enum RpcClientStatus
    {
        Disconnected,
        Connecting,
        Connected,
        SessionReady
    }
    
    public class RpcTcpClient : IRpcPeer
    {
        internal class InnerTcpClient : TcpClient
        {
            public IPEndPoint LastEndpoint => base.lastEndpoint;
            RpcTcpClient parent;

            public InnerTcpClient(RpcTcpClient parent, RpcTcpConfigurationClient configuration) : base(configuration
                .TcpConfiguration)
            {
                this.parent = parent;
            }

            protected override TcpConnection CreateConnection()
            {
                return parent.CreateConnection();
            }

            protected override void OnConnectionOpened(ConnectionOpenedEventArgs args)
            {
                base.OnConnectionOpened(args);
                parent.OnConnectionOpened(args);
            }

            protected override void OnConnectionClosed(ConnectionClosedEventArgs args)
            {
                base.OnConnectionClosed(args);
                parent.OnConnectionClosed(args);
            }

            protected override void PollEvents()
            {
                parent.PollEventsInternal();
                base.PollEvents();
            }
        }

        public string Tag { get; set; }
        public RpcTcpConfigurationClient Configuration => configuration;
        public RpcSession Session { get; private set; }
        public TcpConnectionStatistics Statistics => innerTcpClient?.Connection?.Statistics;
        public RpcClientStatus Status { get; private set; }
        public event DOnSessionOpened OnSessionOpenedEvent;
        public event DOnSessionClosed OnSessionClosedEvent;
        public event DOnClientStatusChanged OnStatusChangedEvent;
        
        readonly InnerTcpClient innerTcpClient;
        readonly RpcTcpConfigurationClient configuration;
        protected readonly ILogger logger;

        public RpcTcpClient(RpcTcpConfigurationClient configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (configuration.Serializer == null)
                throw new ArgumentNullException(nameof(configuration.Serializer));
            if (configuration.SessionFactory == null)
                throw new ArgumentNullException(nameof(configuration.SessionFactory));
            configuration.Lock();
            this.configuration = configuration;
            this.innerTcpClient = new InnerTcpClient(this, configuration);
            this.logger = configuration.LogManager.GetLogger(nameof(RpcTcpClient));
            this.logger.Meta["kind"] = this.GetType().Name;
            this.logger.Meta["tag"] = new RefLogLabel<RpcTcpClient>(this, s => s.Tag);
        }
        
        public void Start()
        {
            innerTcpClient.Start();
        }
        
        public void Shutdown()
        {
            innerTcpClient.Shutdown();
        }

        public Task StartSessionNoAuth()
        {
            return StartSessionNoAuth(default);
        }

        public Task StartSessionNoAuth(CancellationToken cancellationToken)
        {
            RpcTcpConnection connection = innerTcpClient.Connection as RpcTcpConnection;
            if (connection == null)
                throw new InvalidOperationException($"{nameof(RpcTcpClient)} is not connected");
            return connection.StartClientSession(false, null, cancellationToken);
        }

        public Task StartSessionWithAuth(object authObject)
        {
            return StartSessionWithAuth(authObject, default);
        }
        
        public Task StartSessionWithAuth(object authObject, CancellationToken cancellationToken)
        {
            RpcTcpConnection connection = innerTcpClient.Connection as RpcTcpConnection;
            if (connection == null)
                throw new InvalidOperationException($"{nameof(RpcTcpClient)} is not connected");
            return connection.StartClientSession(true, authObject, cancellationToken);
        }

        public Task OpenConnectionAsync(string host, int port)
        {
            return OpenConnectionAsync(host, port, default);
        }

        public async Task OpenConnectionAsync(string host, int port, CancellationToken cancellationToken)
        {
            if (Status != RpcClientStatus.Disconnected)
                throw new InvalidOperationException($"Wrong status {Status}, expected {RpcClientStatus.Disconnected}");
            
            try
            {
                logger.Debug($"Connecting to {host}:{port}");
                ChangeStatus(RpcClientStatus.Connecting);
                await innerTcpClient.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
                RpcTcpConnection connection = (RpcTcpConnection) innerTcpClient.Connection;
                logger.Trace($"Waiting for session...");
                await connection.Start(cancellationToken).ConfigureAwait(false);
                ChangeStatus(RpcClientStatus.Connected);
            }
            catch (Exception ex)
            {
                logger.Error($"Exception on establishing session: {ex}");
                this.CloseInternal();
                throw;
            }
        }
        
        public Task OpenConnectionAsync(IPEndPoint endpoint)
        {
            return OpenConnectionAsync(endpoint, default);
        }
        
        public async Task OpenConnectionAsync(IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            if (Status != RpcClientStatus.Disconnected)
                throw new InvalidOperationException($"Wrong status {Status}, expected {RpcClientStatus.Disconnected}");

            try
            {
                logger.Trace($"Connecting to {endpoint}");
                ChangeStatus(RpcClientStatus.Connecting);
                await innerTcpClient.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
                RpcTcpConnection connection = (RpcTcpConnection) innerTcpClient.Connection;
                logger.Trace($"Waiting for session...");
                await connection.Start(cancellationToken).ConfigureAwait(false);
                ChangeStatus(RpcClientStatus.Connected);
            }
            catch (Exception ex)
            {
                logger.Error($"Exception on establishing session: {ex}");
                this.CloseInternal();
                throw;
            }
        }

        void CloseInternal()
        {
            innerTcpClient.Disconnect();
            ChangeStatus(RpcClientStatus.Disconnected);
        }

        public void Close()
        {
            CloseInternal();
        }

        protected internal virtual RpcTcpConnection CreateConnection()
        {
            RpcTcpConnection connection = new RpcTcpConnection(innerTcpClient, this, configuration);;
            return connection;
        }

        internal void OnConnectionOpened(ConnectionOpenedEventArgs args)
        {
            
        }

        internal void OnConnectionClosed(ConnectionClosedEventArgs args)
        {
            ChangeStatus(RpcClientStatus.Disconnected);
        }

        void IRpcPeer.OnSessionOpened(SessionOpenedEventArgs args)
        {
            this.Session = args.Session;
            ChangeStatus(RpcClientStatus.SessionReady);
            
            configuration.SynchronizeSafe(logger, $"{nameof(RpcTcpClient)}.{nameof(OnSessionClosed)}",
                (state) => OnSessionOpened(state as SessionOpenedEventArgs), args);
            configuration.SynchronizeSafe(logger, $"{nameof(RpcTcpClient)}.{nameof(OnSessionOpenedEvent)}",
                (state) => OnSessionOpenedEvent?.Invoke(state as SessionOpenedEventArgs), args);
        }

        void IRpcPeer.OnSessionClosed(SessionClosedEventArgs args)
        {
            this.Session = null;
            ChangeStatus(RpcClientStatus.Connected);
            configuration.SynchronizeSafe(logger, $"{nameof(RpcTcpClient)}.{nameof(OnSessionClosed)}",
                (state) => OnSessionClosed(state as SessionClosedEventArgs), args);
            configuration.SynchronizeSafe(logger, $"{nameof(RpcTcpClient)}.{nameof(OnSessionClosedEvent)}",
                (state) => OnSessionClosedEvent?.Invoke(state as SessionClosedEventArgs), args);
        }

        protected virtual void OnSessionOpened(SessionOpenedEventArgs args)
        {
            
        }

        protected virtual void OnSessionClosed(SessionClosedEventArgs args)
        {
            
        }
        
        protected virtual void OnStatusChanged(RpcClientStatusChangedEventArgs args)
        {
            
        }

        void ChangeStatus(RpcClientStatus newStatus)
        {
            if (Status == newStatus)
                return;
            logger.Info($"Changed status from {Status} to {newStatus}");
            RpcClientStatusChangedEventArgs args = new RpcClientStatusChangedEventArgs(this, Status, newStatus);
            Status = newStatus;
            
            configuration.SynchronizeSafe(logger, $"{nameof(RpcTcpClient)}.{nameof(OnStatusChanged)}",
                (state) => OnStatusChanged(state as RpcClientStatusChangedEventArgs), args);
            configuration.SynchronizeSafe(logger, $"{nameof(RpcTcpClient)}.{nameof(OnStatusChangedEvent)}",
                (state) => OnStatusChangedEvent?.Invoke(state as RpcClientStatusChangedEventArgs), args);
        }

        protected virtual void PollEvents()
        {
            
        }

        internal void PollEventsInternal()
        {
            PollEvents();
        }
    }
}