using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking;
using Neon.Networking.Udp;
using Neon.Rpc.Net.Events;
using Neon.Rpc.Net.Tcp;
using Neon.Rpc.Net.Tcp.Events;
using Neon.Rpc.Net.Udp.Events;
using ConnectionClosedEventArgs = Neon.Networking.Udp.Events.ConnectionClosedEventArgs;
using ConnectionOpenedEventArgs = Neon.Networking.Udp.Events.ConnectionOpenedEventArgs;
using Dns = Neon.Networking.Dns;

namespace Neon.Rpc.Net.Udp
{
    public class RpcUdpClient
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
        
        /// <summary>
        /// User-defined tag
        /// </summary>
        public string Tag { get; set; }
        /// <summary>
        /// Client configuration
        /// </summary>
        public RpcUdpConfigurationClient Configuration => _configuration;
        /// <summary>
        /// User session
        /// </summary>
        public RpcSessionBase Session => (_innerUdpClient?.Connection as RpcUdpConnection)?.UserSession;
        /// <summary>
        /// Client status
        /// </summary>
        public RpcClientStatus Status { get; private set; }
        /// <summary>
        /// Raised when client status changed
        /// </summary>
        public event DOnRpcUdpClientStatusChanged OnStatusChangedEvent;
        
        readonly InnerUdpClient _innerUdpClient;
        readonly object _statusMutex = new object();
        readonly RpcUdpConfigurationClient _configuration;
        protected readonly ILogger _logger;

        public RpcUdpClient(RpcUdpConfigurationClient configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (configuration.SessionFactory == null)
                throw new ArgumentNullException(nameof(configuration.SessionFactory));
            configuration.Lock();
            this._configuration = configuration;
            _innerUdpClient = new InnerUdpClient(this, configuration);
            _logger = configuration.LogManager.GetLogger(typeof(RpcUdpClient));
        }
        
        protected void CheckStarted()
        {
            if (!_innerUdpClient.IsStarted)
                throw new InvalidOperationException("Please call Start() first");
        }
        
        bool ChangeStatus(RpcClientStatus newStatus)
        {
            return ChangeStatus(newStatus, s => true, out _);
        }

        bool ChangeStatus(RpcClientStatus newStatus, out RpcClientStatus oldStatus)
        {
            return ChangeStatus(newStatus, s => true, out oldStatus);
        }

        bool ChangeStatus(RpcClientStatus newStatus, Func<RpcClientStatus, bool> statusCheck,
            out RpcClientStatus oldStatus)
        {
            lock (_statusMutex)
            {
                oldStatus = Status;
                if (oldStatus == newStatus)
                    return false;
                if (!statusCheck(oldStatus))
                    return false;
                Status = newStatus;
            }

            _logger.Info($"Status changed from {oldStatus} to {newStatus}");

            RpcUdpClientStatusChangedEventArgs args = new RpcUdpClientStatusChangedEventArgs(this, Status, newStatus);

            _configuration.SynchronizeSafe(_logger, $"{nameof(RpcUdpClient)}.{nameof(OnStatusChanged)}",
                state => OnStatusChanged(state as RpcUdpClientStatusChangedEventArgs), args);
            _configuration.SynchronizeSafe(_logger, $"{nameof(RpcUdpClient)}.{nameof(OnStatusChangedEvent)}",
                state => OnStatusChangedEvent?.Invoke(state as RpcUdpClientStatusChangedEventArgs), args);

            return true;
        }
        
        /// <summary>
        /// Start an internal network thread
        /// </summary>
        public void Start()
        {
            _innerUdpClient.Start();
        }
        
        /// <summary>
        /// Disconnects and shutdown the client
        /// </summary>
        public void Shutdown()
        {
            _innerUdpClient.Shutdown();
        }

        /// <summary>
        /// Starts a new connection to the server
        /// </summary>
        /// <param name="host">IP address or domain of the server</param>
        /// <param name="port">Server port</param>
        /// <param name="ipAddressSelectionRules">If destination host resolves to a few ap addresses, which we should take</param>
        /// <param name="authenticationInfo">Authentication parameters</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>An established user session</returns>
        public async Task<RpcSessionBase> StartSessionAsync(string host, int port, IPAddressSelectionRules ipAddressSelectionRules, AuthenticationInfo authenticationInfo, CancellationToken cancellationToken)
        {
            if (authenticationInfo == null) throw new ArgumentNullException(nameof(authenticationInfo));
            CheckStarted();
            _logger.Debug($"Resolving {host} to ip address...");
            IPEndPoint endPoint =
                await Dns.ResolveEndpoint(host, port, ipAddressSelectionRules);
            _logger.Debug($"Resolved {host} to {endPoint.Address}");
            return await StartSessionAsync(endPoint, authenticationInfo, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Starts a new connection to the server
        /// </summary>
        /// <param name="endpoint">IP endpoint of a desired host</param>
        /// <param name="authenticationInfo">Authentication parameters</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>An established user session</returns>
        public async Task<RpcSessionBase> StartSessionAsync(IPEndPoint endpoint, AuthenticationInfo authenticationInfo, CancellationToken cancellationToken)
        {
            CheckStarted();
            if (!ChangeStatus(RpcClientStatus.Connecting, s => s == RpcClientStatus.Disconnected,
                    out RpcClientStatus oldStatus))
                throw new InvalidOperationException(
                    $"Wrong {nameof(RpcUdpClient)} status {oldStatus}, {RpcClientStatus.Disconnected} expected");
            try
            {
                _logger.Debug($"Connecting to {endpoint}");
                await _innerUdpClient.ConnectAsync(endpoint, cancellationToken)
                    .ConfigureAwait(false);
                RpcUdpConnection connection = (RpcUdpConnection) _innerUdpClient.Connection;
                
                _logger.Trace($"Handshaking...");
                await connection.Controller.Handshake(cancellationToken).ConfigureAwait(false);
                _logger.Trace($"Authenticating...");
                await connection.Controller
                    .Authenticate(authenticationInfo, cancellationToken)
                    .ConfigureAwait(false);
                _logger.Trace($"Creating user session...");
                await connection.Controller.CreateUserSession(cancellationToken).ConfigureAwait(false);
                
                if (!ChangeStatus(RpcClientStatus.Connected, s => s == RpcClientStatus.Connecting, out oldStatus))
                    throw new InvalidOperationException("Connection reset");

                _logger.Debug($"Connected to {endpoint}");
                return connection.UserSession;
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception on establishing session: {ex}");
                await CloseAsync();
                throw;
            }
        }

        /// <summary>
        /// Closing any established session/connection
        /// </summary>
        public Task CloseAsync()
        {
            return _innerUdpClient.DisconnectAsync();
        }

        protected internal virtual RpcUdpConnection CreateConnection()
        {
            RpcUdpConnection connection = new RpcUdpConnection(_innerUdpClient, _configuration);
            return connection;
        }

        internal void OnConnectionOpened(ConnectionOpenedEventArgs args)
        {
            
        }

        internal void OnConnectionClosed(ConnectionClosedEventArgs args)
        {
            ChangeStatus(RpcClientStatus.Disconnected);
        }

        protected virtual void OnStatusChanged(RpcUdpClientStatusChangedEventArgs args)
        {
            
        }

    }
}