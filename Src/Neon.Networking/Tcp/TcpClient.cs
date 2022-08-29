using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Neon.Networking.Tcp.Events;
using Neon.Logging;
using Neon.Util;

namespace Neon.Networking.Tcp
{
    public enum TcpClientStatus
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2
    }
    
    public class TcpClient : TcpPeer
    {
        /// <summary>
        /// A client status
        /// </summary>
        public TcpClientStatus Status => status;
        /// <summary>
        /// Returns an instance of the current connection. Can be null
        /// </summary>
        public TcpConnection Connection { get; private set; }

        private protected override ILogger Logger => logger;

        new readonly TcpConfigurationClient configuration;
        readonly ILogger logger;
        
        TcpClientStatus status;
        long connectionId;
        object statusMutex = new object();
        
        protected IPEndPoint lastEndpoint;

        public TcpClient(TcpConfigurationClient configuration) : base(configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            this.configuration = configuration;
            this.logger = configuration.LogManager.GetLogger(nameof(TcpClient));
            this.logger.Meta.Add("kind", this.GetType().Name);
            status = TcpClientStatus.Disconnected;
        }
        
        bool ChangeStatus(TcpClientStatus newStatus)
        {
            return ChangeStatus(newStatus, (s) => true, out _);
        }

        bool ChangeStatus(TcpClientStatus newStatus, out TcpClientStatus oldStatus)
        {
            return ChangeStatus(newStatus, (s) => true, out oldStatus);
        }
        
        bool ChangeStatus(TcpClientStatus newStatus, Func<TcpClientStatus, bool> statusCheck, out TcpClientStatus oldStatus)
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

            logger.Info($"{nameof(TcpClient)} status changed from {oldStatus} to {newStatus}");

            ClientStatusChangedEventArgs args = new ClientStatusChangedEventArgs(newStatus, this);
            
            configuration.SynchronizeSafe(logger, $"{nameof(TcpClient)}.{nameof(OnClientStatusChanged)}",
                (state) => OnClientStatusChanged(state as ClientStatusChangedEventArgs), args
            );

            return true;
        }

        private protected override void PollEventsInternal()
        {
            Connection?.PollEventsInternal();
        }

        protected virtual void OnClientStatusChanged(ClientStatusChangedEventArgs args)
        {

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
            return ConnectAsync(host, port, ipAddressSelectionRules, default);
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
            
            cancellationToken.ThrowIfCancellationRequested();

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
        public virtual async Task ConnectAsync(IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            CheckStarted();
            if (!ChangeStatus(TcpClientStatus.Connecting, s => s == TcpClientStatus.Disconnected,
                    out TcpClientStatus oldStatus))
                throw new InvalidOperationException(
                    $"Wrong {nameof(TcpClient)} status {oldStatus}, {TcpClientStatus.Disconnected} expected");

            Socket clientSocket_ = null;
            try
            {
                logger.Info($"Starting the connection to {endpoint}");
                clientSocket_ = this.GetNewSocket(endpoint.AddressFamily);
                this.SetSocketOptions(clientSocket_);
                await clientSocket_.ConnectAsync(endpoint)
                    .TimeoutAfter(configuration.ConnectTimeout, cancellationToken)
                    .ConfigureAwait(false);
                var connection = this.CreateConnection();
                
                connection.CheckParent(this);
                long newId = Interlocked.Increment(ref connectionId);

                connection.Init(newId, clientSocket_, true);
                connection.StartReceive();

                if (!connection.Connected)
                    throw new InvalidOperationException("Connection reset");

                if (!ChangeStatus(TcpClientStatus.Connected, s => s == TcpClientStatus.Connecting, out oldStatus))
                    throw new InvalidOperationException("Connection reset");
                
                this.Connection = connection;
            }
            catch
            {
                if (clientSocket_ != null)
                    clientSocket_.Dispose();
                DisconnectInternal();

                throw;
            }
            finally
            {
                lastEndpoint = endpoint;
            }
        }

        internal override void OnConnectionClosedInternal(TcpConnection tcpConnection, Exception ex)
        {
            Connection = null;
            tcpConnection.Dispose();
            ChangeStatus(TcpClientStatus.Disconnected);
            base.OnConnectionClosedInternal(tcpConnection, ex);
        }

        /// <summary>
        /// Disconnects & shutdown the client
        /// </summary>
        public override void Shutdown()
        {
            logger.Info($"{nameof(TcpClient)} shutdown");
            Disconnect();
            base.Shutdown();
        }

        void DisconnectInternal()
        {
            if (status == TcpClientStatus.Disconnected)
                return;

            var connection = Connection;
            if (connection != null)
            {
                connection.Close();
                Connection = null;
            }
            ChangeStatus(TcpClientStatus.Disconnected);
        }

        /// <summary>
        /// Terminate the current connection
        /// </summary>
        public void Disconnect()
        {
            CheckStarted();
            DisconnectInternal();
        }
    }
}
