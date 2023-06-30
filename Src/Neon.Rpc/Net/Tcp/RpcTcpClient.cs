using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking;
using Neon.Networking.Tcp;
using Neon.Networking.Tcp.Events;
using Neon.Rpc.Net.Events;
using Neon.Rpc.Net.Tcp.Events;
using Dns = Neon.Networking.Dns;

namespace Neon.Rpc.Net.Tcp
{
    public enum RpcClientStatus
    {
        Disconnected,
        Connecting,
        Connected
    }
    
    public class RpcTcpClient
    {
        internal class InnerTcpClient : TcpClient
        {
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
                parent.OnConnectionOpenedInternal(args);
            }

            protected override void OnConnectionClosed(ConnectionClosedEventArgs args)
            {
                base.OnConnectionClosed(args);
                parent.OnConnectionClosedInternal(args);
            }

            protected override void PollEvents()
            {
                parent.PollEventsInternal();
                base.PollEvents();
            }
        }

        /// <summary>
        /// User-defined tag
        /// </summary>
        public string Tag { get; set; }
        /// <summary>
        /// Client configuration
        /// </summary>
        public RpcTcpConfigurationClient Configuration => _configuration;

        /// <summary>
        /// User session
        /// </summary>
        public RpcSessionBase Session => (_innerTcpClient?.Connection as RpcTcpConnection)?.UserSession;
        /// <summary>
        /// Client connection statistics
        /// </summary>
        public TcpConnectionStatistics Statistics => _innerTcpClient?.Connection?.Statistics;
        /// <summary>
        /// Client status
        /// </summary>
        public RpcClientStatus Status { get; private set; }
        /// <summary>
        /// Raised when client status changed
        /// </summary>
        public event DOnRpcTcpClientStatusChanged OnStatusChangedEvent;
        
        readonly InnerTcpClient _innerTcpClient;
        readonly object _statusMutex = new object();
        readonly RpcTcpConfigurationClient _configuration;
        protected readonly ILogger _logger;

        public RpcTcpClient(RpcTcpConfigurationClient configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (configuration.SessionFactory == null)
                throw new ArgumentNullException(nameof(configuration.SessionFactory));
            configuration.Lock();
            this._configuration = configuration;
            _innerTcpClient = new InnerTcpClient(this, configuration);
            _logger = configuration.LogManager.GetLogger(typeof(RpcTcpClient));
            _logger.Meta["tag"] = new RefLogLabel<RpcTcpClient>(this, s => s.Tag);
        }
        
        protected void CheckStarted()
        {
            if (!_innerTcpClient.IsStarted)
                throw new InvalidOperationException("Please call Start() first");
        }
        
        /// <summary>
        /// Start an internal network thread
        /// </summary>
        public void Start()
        {
            _innerTcpClient.Start();
        }
        
        /// <summary>
        /// Disconnects and shutdown the client
        /// </summary>
        public void Shutdown()
        {
            _innerTcpClient.Shutdown();
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
            if (authenticationInfo == null) throw new ArgumentNullException(nameof(authenticationInfo));
            CheckStarted();
            if (!ChangeStatus(RpcClientStatus.Connecting, s => s == RpcClientStatus.Disconnected,
                    out RpcClientStatus oldStatus))
                throw new InvalidOperationException(
                    $"Wrong {nameof(RpcTcpClient)} status {oldStatus}, {RpcClientStatus.Disconnected} expected");

            try
            {
                _logger.Trace($"Connecting to {endpoint}");
                RpcTcpConnection connection =
                    (RpcTcpConnection) await _innerTcpClient.ConnectAsync(endpoint, cancellationToken)
                        .ConfigureAwait(false);


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
                CloseInternal();
                throw;
            }
        }

        void CloseInternal()
        {
            _innerTcpClient.Disconnect();
            ChangeStatus(RpcClientStatus.Disconnected);
        }

        /// <summary>
        /// Closing any established session/connection
        /// </summary>
        public void Close()
        {
            CloseInternal();
        }

        internal RpcTcpConnection CreateConnection()
        {
            RpcTcpConnection connection = new RpcTcpConnection(_innerTcpClient, _configuration);;
            return connection;
        }

        internal void OnConnectionOpenedInternal(ConnectionOpenedEventArgs args)
        {

        }

        internal void OnConnectionClosedInternal(ConnectionClosedEventArgs args)
        {
            ChangeStatus(RpcClientStatus.Disconnected);
        }

        protected virtual void OnStatusChanged(RpcTcpClientStatusChangedEventArgs args)
        {
            
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

            RpcTcpClientStatusChangedEventArgs args = new RpcTcpClientStatusChangedEventArgs(this, Status, newStatus);

            _configuration.SynchronizeSafe(_logger, $"{nameof(RpcTcpClient)}.{nameof(OnStatusChanged)}",
                state => OnStatusChanged(state as RpcTcpClientStatusChangedEventArgs), args);
            _configuration.SynchronizeSafe(_logger, $"{nameof(RpcTcpClient)}.{nameof(OnStatusChangedEvent)}",
                state => OnStatusChangedEvent?.Invoke(state as RpcTcpClientStatusChangedEventArgs), args);

            return true;
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