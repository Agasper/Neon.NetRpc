using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Neon.Rpc.Net.Events;
using Neon.Logging;
using Neon.Networking;
using Neon.Networking.Udp;
using Neon.Networking.Udp.Events;
using Neon.Rpc.Net.Tcp;

namespace Neon.Rpc.Net.Udp
{
    public class RpcUdpClient : IRpcPeer
    {
        internal class InnerUdpClient : UdpClient
        {
            RpcUdpClient parent;

            public InnerUdpClient(RpcUdpClient parent, RpcUdpConfigurationClient configuration) : base(configuration.UdpConfiguration)
            {
                this.parent = parent;
            }
            
            protected override UdpConnection CreateConnection()
            {
                return parent.CreateConnection();
            }

            protected override void OnConnectionClosed(ConnectionClosedEventArgs args)
            {
                base.OnConnectionClosed(args);
                parent.OnConnectionClosed(args);
            }

            protected override void OnConnectionOpened(ConnectionOpenedEventArgs args)
            {
                base.OnConnectionOpened(args);
                parent.OnConnectionOpened(args);
            }
        }
        
        public RpcUdpConfigurationClient Configuration => configuration;
        public RpcSession Session { get; private set; }
        public RpcClientStatus Status { get; private set; }
        public bool Ready => Session != null;
        public event DOnSessionOpened OnSessionOpenedEvent;
        public event DOnSessionClosed OnSessionClosedEvent;
        public event DOnClientStatusChanged OnStatusChangedEvent;
        
        readonly InnerUdpClient innerUdpClient;
        readonly RpcUdpConfigurationClient configuration;
        protected readonly ILogger logger;

        public RpcUdpClient(RpcUdpConfigurationClient configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (configuration.Serializer == null)
                throw new ArgumentNullException(nameof(configuration.Serializer));
            configuration.Lock();
            this.configuration = configuration;
            this.innerUdpClient = new InnerUdpClient(this, configuration);
            this.logger = configuration.LogManager.GetLogger(nameof(RpcUdpClient));
            this.logger.Meta["kind"] = this.GetType().Name;
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
        
        public void Start()
        {
            innerUdpClient.Start();
        }
        
        public void Shutdown()
        {
            innerUdpClient.Shutdown();
        }
        
        public Task StartSessionNoAuth()
        {
            return StartSessionNoAuth(default);
        }

        public Task StartSessionNoAuth(CancellationToken cancellationToken)
        {
            RpcUdpConnection connection = innerUdpClient.Connection as RpcUdpConnection;
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
            RpcUdpConnection connection = innerUdpClient.Connection as RpcUdpConnection;
            if (connection == null)
                throw new InvalidOperationException($"{nameof(RpcTcpClient)} is not connected");
            return connection.StartClientSession(true, authObject, cancellationToken);
        }

        public Task OpenConnectionAsync(string host, int port, IPAddressSelectionRules ipAddressSelectionRules = default)
        {
            return OpenConnectionAsync(host, port, ipAddressSelectionRules,default);
        }
        
        public async Task OpenConnectionAsync(string host, int port, IPAddressSelectionRules ipAddressSelectionRules, CancellationToken cancellationToken)
        {
            if (Session != null)
                throw new InvalidOperationException("Session already started");
            
            try
            {
                logger.Debug($"Connecting to {host}:{port}");
                ChangeStatus(RpcClientStatus.Connecting);
                await innerUdpClient.ConnectAsync(host, port, ipAddressSelectionRules, cancellationToken)
                    .ConfigureAwait(false);
                RpcUdpConnection connection = (RpcUdpConnection) innerUdpClient.Connection;
                logger.Trace($"Waiting for session...");
                await connection.Start(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await this.CloseAsync();
                logger.Error($"Exception on establishing session: {ex}");
                throw;
            }
        }

        public Task OpenConnectionAsync(IPEndPoint endpoint)
        {
            return OpenConnectionAsync(endpoint, default);
        }

        public async Task OpenConnectionAsync(IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            if (Session != null)
                throw new InvalidOperationException("Session already started");
            try
            {
                logger.Debug($"Connecting to {endpoint}");
                ChangeStatus(RpcClientStatus.Connecting);
                await innerUdpClient.ConnectAsync(endpoint, cancellationToken)
                    .ConfigureAwait(false);
                RpcUdpConnection connection = (RpcUdpConnection) innerUdpClient.Connection;
                logger.Trace($"Waiting for session...");
                await connection.Start(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.Error($"Exception on establishing session: {ex}");
                await this.CloseAsync();
                throw;
            }
        }

        public Task CloseAsync()
        {
            return innerUdpClient.DisconnectAsync();
        }

        protected internal virtual RpcUdpConnection CreateConnection()
        {
            RpcUdpConnection connection = new RpcUdpConnection(innerUdpClient, this, configuration);
            return connection;
        }

        internal void OnConnectionOpened(ConnectionOpenedEventArgs args)
        {
            ChangeStatus(RpcClientStatus.Connected);
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
            
            configuration.SynchronizeSafe(logger, $"{nameof(RpcUdpClient)}.{nameof(OnSessionClosed)}", (state) =>
            {
                OnSessionClosed(state as SessionClosedEventArgs);
            }, args);
            configuration.SynchronizeSafe(logger, $"{nameof(RpcUdpClient)}.{nameof(OnSessionClosedEvent)}", (state) =>
            {
                OnSessionClosedEvent?.Invoke(state as SessionClosedEventArgs);
            }, args);
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

    }
}