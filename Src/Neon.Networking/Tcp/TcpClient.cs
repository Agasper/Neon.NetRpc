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
        public TcpClientStatus Status => status;
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

        public Task ConnectAsync(string host, int port)
        {
            return ConnectAsync(host, port, default);
        }
        
        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            CheckStarted();
            IPAddress ip = IPAddress.Any;
            if (IPAddress.TryParse(host, out IPAddress ip_))
            {
                ip = ip_;
            }
            else
            {
                logger.Debug($"Resolving {host} to ip address...");
                var addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
                ip = addresses.First(addr => addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                logger.Debug($"Resolved {host} to {ip}");
            }

            IPEndPoint endpoint = new IPEndPoint(ip, port);
            await ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
        }
        
        public Task ConnectAsync(IPEndPoint endpoint)
        {
            return ConnectAsync(endpoint, default);
        }

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
                clientSocket_ = this.GetNewSocket();
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

        public void Disconnect()
        {
            CheckStarted();
            DisconnectInternal();
        }
    }
}
