using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking.Tcp.Events;
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
        ///     A client status
        /// </summary>
        public TcpClientStatus Status { get; private set; }

        /// <summary>
        ///     Returns an instance of the current connection. Can be null
        /// </summary>
        public TcpConnection Connection { get; private set; }

        private protected override ILogger Logger => _logger;
        new readonly TcpConfigurationClient _configuration;

        readonly object _statusMutex = new object();
        readonly ILogger _logger;
        long _connectionId;

        public TcpClient(TcpConfigurationClient configuration) : base(configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            _configuration = configuration;
            _logger = configuration.LogManager.GetLogger(typeof(TcpClient));
            Status = TcpClientStatus.Disconnected;
        }

        bool ChangeStatus(TcpClientStatus newStatus)
        {
            return ChangeStatus(newStatus, s => true, out _);
        }

        bool ChangeStatus(TcpClientStatus newStatus, out TcpClientStatus oldStatus)
        {
            return ChangeStatus(newStatus, s => true, out oldStatus);
        }

        bool ChangeStatus(TcpClientStatus newStatus, Func<TcpClientStatus, bool> statusCheck,
            out TcpClientStatus oldStatus)
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

            _logger.Info($"{nameof(TcpClient)} status changed from {oldStatus} to {newStatus}");

            var args = new ClientStatusChangedEventArgs(newStatus, this);

            _configuration.SynchronizeSafe(_logger, $"{nameof(TcpClient)}.{nameof(OnClientStatusChanged)}",
                state => OnClientStatusChanged(state as ClientStatusChangedEventArgs), args
            );

            return true;
        }

        private protected override void PollEventsInternal()
        {
            Connection?.PollEvents();
        }

        protected virtual void OnClientStatusChanged(ClientStatusChangedEventArgs args)
        {
        }


        /// <summary>
        ///     Start the asynchronous operation to establish a connection to the specified host and port
        /// </summary>
        /// <param name="host">IP or domain of a desired host</param>
        /// <param name="port">Destination port</param>
        /// <param name="ipAddressSelectionRules">If destination host resolves to a few ap addresses, which we should take</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task<TcpConnection> ConnectAsync(string host, int port,
            IPAddressSelectionRules ipAddressSelectionRules, CancellationToken cancellationToken)
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
        public virtual async Task<TcpConnection> ConnectAsync(IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            CheckStarted();
            if (!ChangeStatus(TcpClientStatus.Connecting, s => s == TcpClientStatus.Disconnected,
                    out TcpClientStatus oldStatus))
                throw new InvalidOperationException(
                    $"Wrong {nameof(TcpClient)} status {oldStatus}, {TcpClientStatus.Disconnected} expected");

            Socket clientSocket_ = null;
            try
            {
                _logger.Info($"Starting the connection to {endpoint}");
                clientSocket_ = GetNewSocket(endpoint.AddressFamily);
                SetSocketOptions(clientSocket_);
                await clientSocket_.ConnectAsync(endpoint)
                    .TimeoutAfter(_configuration.ConnectTimeout, cancellationToken)
                    .ConfigureAwait(false);
                TcpConnection connection = CreateConnection();

                connection.CheckParent(this);
                long newId = Interlocked.Increment(ref _connectionId);

                connection.Init(newId, clientSocket_, true);
                connection.StartReceive();

                if (!connection.Connected)
                    throw new InvalidOperationException("Connection reset");

                if (!ChangeStatus(TcpClientStatus.Connected, s => s == TcpClientStatus.Connecting, out oldStatus))
                    throw new InvalidOperationException("Connection reset");

                Connection = connection;
                return connection;
            }
            catch
            {
                if (clientSocket_ != null)
                    clientSocket_.Dispose();
                DisconnectInternal();

                throw;
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
        ///     Disconnects and shutdown the client
        /// </summary>
        public override void Shutdown()
        {
            _logger.Info($"{nameof(TcpClient)} shutdown");
            Disconnect();
            base.Shutdown();
        }

        void DisconnectInternal()
        {
            if (Status == TcpClientStatus.Disconnected)
                return;

            TcpConnection connection = Connection;
            if (connection != null)
            {
                connection.Close();
                Connection = null;
            }

            ChangeStatus(TcpClientStatus.Disconnected);
        }

        /// <summary>
        ///     Terminate the current connection
        /// </summary>
        public void Disconnect()
        {
            CheckStarted();
            DisconnectInternal();
        }
    }
}