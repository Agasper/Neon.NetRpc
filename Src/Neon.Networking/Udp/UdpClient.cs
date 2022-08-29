using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Neon.Networking.Udp.Events;
using Neon.Networking.Udp.Messages;
using Neon.Logging;
using Neon.Networking.Messages;
using Neon.Networking.Udp.Exceptions;

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
        /// Current connection statistics
        /// </summary>
        public UdpConnectionStatistics Statistics => Connection?.Statistics;
        /// <summary>
        /// A client status
        /// </summary>
        public UdpClientStatus Status => status;
        /// <summary>
        /// Returns an instance of the current connection. Can be null
        /// </summary>
        public UdpConnection Connection => connection;
        readonly UdpConfigurationClient configuration;

        private protected override ILogger Logger => logger;

        volatile UdpConnection connection;
        volatile UdpClientStatus status;
        object statusMutex = new object();
        ILogger logger;

        public UdpClient(UdpConfigurationClient configuration) : base(configuration)
        {
            this.configuration = configuration;
            this.logger = configuration.LogManager.GetLogger(nameof(UdpClient));
            this.logger.Meta["kind"] = this.GetType().Name;
        }
        
        bool ChangeStatus(UdpClientStatus newStatus)
        {
            return ChangeStatus(newStatus, (s) => true, out _);
        }

        bool ChangeStatus(UdpClientStatus newStatus, out UdpClientStatus oldStatus)
        {
            return ChangeStatus(newStatus, (s) => true, out oldStatus);
        }
        
        bool ChangeStatus(UdpClientStatus newStatus, Func<UdpClientStatus, bool> statusCheck, out UdpClientStatus oldStatus)
        {
            lock (statusMutex)
            {
                oldStatus = this.status;
                if (oldStatus == newStatus)
                    return false;
                if (!statusCheck(oldStatus))
                    return false;
                this.status = newStatus;
            }

            logger.Info($"{nameof(UdpClient)} status changed from {oldStatus} to {newStatus}");

            ClientStatusChangedEventArgs args = new ClientStatusChangedEventArgs(newStatus, this);
            
            configuration.SynchronizeSafe(logger, $"{nameof(UdpClient)}.{nameof(OnClientStatusChanged)}",
                (state) => OnClientStatusChanged(state as ClientStatusChangedEventArgs), args
            );

            return true;
        }
        
        protected virtual void OnClientStatusChanged(ClientStatusChangedEventArgs args)
        {

        }
        
        internal override void OnConnectionClosedInternal(ConnectionClosedEventArgs args)
        {
            args.Connection.Dispose();
            this.connection = null;
            ChangeStatus(UdpClientStatus.Disconnected);
            base.OnConnectionClosedInternal(args);
        }

        private protected override void PollEventsInternal()
        {
            base.PollEventsInternal();
            Connection?.PollEvents();
        }

        /// <summary>
        /// Start the asynchronous operation to establish a connection to the specified host and port
        /// </summary>
        /// <param name="host">IP or domain of a desired host</param>
        /// <param name="port">Destination port</param>
        /// <param name="ipAddressSelectionRules">If destination host resolves to a few ap addresses, which we should take</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task ConnectAsync(string host, int port, IPAddressSelectionRules ipAddressSelectionRules = default)
        {
            return ConnectAsync(host, port,ipAddressSelectionRules, default);
        }
        
        /// <summary>
        /// Start the asynchronous operation to establish a connection to the specified host and port
        /// </summary>
        /// <param name="host">IP or domain of a desired host</param>
        /// <param name="port">Destination port</param>
        /// <param name="ipAddressSelectionRules">If destination host resolves to a few ap addresses, which we should take</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ConnectAsync(string host, int port, IPAddressSelectionRules ipAddressSelectionRules, CancellationToken cancellationToken)
        {
            CheckStarted();
            IPAddress ip;
            if (!IPAddress.TryParse(host, out ip))
            {
                ip = null;
                logger.Debug($"Resolving {host} to ip address...");
                var addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);

                switch (ipAddressSelectionRules)
                {
                    case IPAddressSelectionRules.OnlyIpv4:
                        ip = addresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork);
                        break;
                    case IPAddressSelectionRules.OnlyIpv6:
                        ip = addresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetworkV6);
                        break;
                    case IPAddressSelectionRules.PreferIpv4:
                        ip = addresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork);
                        if (ip == null)
                            ip = addresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetworkV6);
                        break;
                    case IPAddressSelectionRules.PreferIpv6: 
                        ip = addresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetworkV6);
                        if (ip == null)
                            ip = addresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork);
                        break;
                }

                if (ip == null)
                    throw new InvalidOperationException($"Couldn't resolve suitable ip address for the host {host}");

                logger.Debug($"Resolved {host} to {ip}");
            }

            IPEndPoint endpoint = new IPEndPoint(ip, port);
            await ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Start the asynchronous operation to establish a connection to the specified ip endpoint
        /// </summary>
        /// <param name="endpoint">IP endpoint of a desired host</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task ConnectAsync(IPEndPoint endpoint)
        {
            return ConnectAsync(endpoint, default);
        }

        /// <summary>
        /// Start the asynchronous operation to establish a connection to the specified ip endpoint
        /// </summary>
        /// <param name="endpoint">IP endpoint of a desired host</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ConnectAsync(IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            CheckStarted();
            if (!ChangeStatus(UdpClientStatus.Connecting, s => s == UdpClientStatus.Disconnected, out UdpClientStatus oldStatus))
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
                var connection_ = CreateConnection();
                connection_.Init(new UdpNetEndpoint(endpoint), true);
                this.connection = connection_;
                await connection_.Connect(cancellationToken).ConfigureAwait(false);
                
                if (!ChangeStatus(UdpClientStatus.Connected, s => s == UdpClientStatus.Connecting, out oldStatus))
                    throw new ConnectionException(DisconnectReason.ClosedByThisPeer, "Connection was closed prematurely");
            }
            catch (Exception)
            {
                await DisconnectAsync().ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Terminate the current connection
        /// </summary>
        public async Task DisconnectAsync()
        {
            CheckStarted();
            var connection_ = this.connection;
            if (connection_ != null)
            {
                ChangeStatus(UdpClientStatus.Disconnecting);
                await connection_.CloseAsync().ConfigureAwait(false);
            }

            DestroySocket();
            ChangeStatus(UdpClientStatus.Disconnected);
        }

        /// <summary>
        /// Terminate the current connections & shutdown the client
        /// </summary>
        public override void Shutdown()
        {
            var connection_ = this.connection;
            if (connection_ != null)
            {
                connection_.Dispose();
            }

            this.connection = null;
            ChangeStatus(UdpClientStatus.Disconnected);
            base.Shutdown();
        }

        private protected override void OnDatagram(Datagram datagram, UdpNetEndpoint remoteEndpoint)
        {
            Connection?.OnDatagram(datagram);
        }
    }
}
