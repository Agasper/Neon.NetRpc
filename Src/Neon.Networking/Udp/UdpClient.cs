using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking.Udp.Events;
using Neon.Networking.Udp.Exceptions;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp
{
    public enum UdpClientStatus
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
        Disconnecting = 3
    }

    public class UdpClient : UdpPeer
    {
        /// <summary>
        ///     Current connection statistics
        /// </summary>
        public UdpConnectionStatistics Statistics => Connection?.Statistics;

        /// <summary>
        ///     A client status
        /// </summary>
        public UdpClientStatus Status => _status;

        /// <summary>
        ///     Returns an instance of the current connection. Can be null
        /// </summary>
        public UdpConnection Connection => _connection;

        private protected override ILogger Logger => _logger;
        readonly UdpConfigurationClient _configuration;
        readonly ILogger _logger;
        readonly object _statusMutex = new object();

        volatile UdpConnection _connection;
        volatile UdpClientStatus _status;

        public UdpClient(UdpConfigurationClient configuration) : base(configuration)
        {
            _configuration = configuration;
            _logger = configuration.LogManager.GetLogger(typeof(UdpClient));
        }

        bool ChangeStatus(UdpClientStatus newStatus)
        {
            return ChangeStatus(newStatus, s => true, out _);
        }

        bool ChangeStatus(UdpClientStatus newStatus, out UdpClientStatus oldStatus)
        {
            return ChangeStatus(newStatus, s => true, out oldStatus);
        }

        bool ChangeStatus(UdpClientStatus newStatus, Func<UdpClientStatus, bool> statusCheck,
            out UdpClientStatus oldStatus)
        {
            lock (_statusMutex)
            {
                oldStatus = _status;
                if (oldStatus == newStatus)
                    return false;
                if (!statusCheck(oldStatus))
                    return false;
                _status = newStatus;
            }

            _logger.Info($"{nameof(UdpClient)} status changed from {oldStatus} to {newStatus}");

            var args = new ClientStatusChangedEventArgs(newStatus, this);

            _configuration.SynchronizeSafe(_logger, $"{nameof(UdpClient)}.{nameof(OnClientStatusChanged)}",
                state => OnClientStatusChanged(state as ClientStatusChangedEventArgs), args
            );

            return true;
        }

        protected virtual void OnClientStatusChanged(ClientStatusChangedEventArgs args)
        {
        }

        internal override void OnConnectionClosedInternal(ConnectionClosedEventArgs args)
        {
            args.Connection.Dispose();
            _connection = null;
            ChangeStatus(UdpClientStatus.Disconnected);
            base.OnConnectionClosedInternal(args);
        }

        private protected override void PollEventsInternal()
        {
            base.PollEventsInternal();
            Connection?.PollEventsInternal();
        }

        /// <summary>
        ///     Start the asynchronous operation to establish a connection to the specified host and port
        /// </summary>
        /// <param name="host">IP or domain of a desired host</param>
        /// <param name="port">Destination port</param>
        /// <param name="ipAddressSelectionRules">If destination host resolves to a few ap addresses, which we should take</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task<UdpConnection> ConnectAsync(string host, int port, IPAddressSelectionRules ipAddressSelectionRules,
            CancellationToken cancellationToken)
        {
            CheckStarted();
            _logger.Debug($"Resolving {host} to ip address...");
            var endpoint = await Dns.ResolveEndpoint(host, port, ipAddressSelectionRules);
            _logger.Debug($"Resolved {host} to {endpoint.Address}");
            return await ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     Start the asynchronous operation to establish a connection to the specified ip endpoint
        /// </summary>
        /// <param name="endpoint">IP endpoint of a desired host</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task<UdpConnection> ConnectAsync(IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            CheckStarted();
            if (!ChangeStatus(UdpClientStatus.Connecting, s => s == UdpClientStatus.Disconnected,
                    out UdpClientStatus oldStatus))
                throw new InvalidOperationException(
                    $"Wrong {nameof(UdpClient)} status {oldStatus}, {UdpClientStatus.Disconnected} expected");
            try
            {
                if (endpoint.AddressFamily == AddressFamily.InterNetwork)
                    Bind(new IPEndPoint(IPAddress.Any, 0));
                else if (endpoint.AddressFamily == AddressFamily.InterNetworkV6)
                    Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
                else
                    throw new ArgumentException($"Invalid address family of the endpoint {endpoint.AddressFamily}");
                UdpConnection connection_ = CreateConnection();
                connection_.Init(new UdpNetEndpoint(endpoint), true);
                _connection = connection_;
                await connection_.Connect(cancellationToken).ConfigureAwait(false);

                if (!ChangeStatus(UdpClientStatus.Connected, s => s == UdpClientStatus.Connecting, out oldStatus))
                    throw new ConnectionException(DisconnectReason.ClosedByThisPeer,
                        "Connection was closed prematurely");

                return _connection;
            }
            catch (Exception)
            {
                Disconnect();
                throw;
            }
        }

        /// <summary>
        ///     Terminate the current connection
        /// </summary>
        public async Task DisconnectAsync()
        {
            CheckStarted();
            UdpConnection connection_ = _connection;
            if (connection_ != null)
            {
                ChangeStatus(UdpClientStatus.Disconnecting);
                await connection_.CloseAsync().ConfigureAwait(false);
            }

            DestroySocket();
            ChangeStatus(UdpClientStatus.Disconnected);
        }

        /// <summary>
        ///     Terminate the current connection
        /// </summary>
        void Disconnect()
        {
            CheckStarted();
            UdpConnection connection_ = _connection;
            if (connection_ != null) connection_.CloseImmediately();

            DestroySocket();
            ChangeStatus(UdpClientStatus.Disconnected);
        }

        /// <summary>
        ///     Terminate the current connections and shutdown the client
        /// </summary>
        public override void Shutdown()
        {
            UdpConnection connection_ = _connection;
            if (connection_ != null) connection_.Dispose();

            _connection = null;
            ChangeStatus(UdpClientStatus.Disconnected);
            base.Shutdown();
        }

        private protected override void OnDatagram(Datagram datagram, UdpNetEndpoint remoteEndpoint)
        {
            Connection?.OnDatagram(datagram);
        }
    }
}