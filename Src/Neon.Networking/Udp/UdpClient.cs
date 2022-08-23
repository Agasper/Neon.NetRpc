using System;
using System.Linq;
using System.Net;
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
        public UdpConnectionStatistics Statistics => Connection?.Statistics;
        public new UdpConfigurationClient Configuration => configuration;
        public UdpClientStatus Status => status;
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

        public async Task ConnectAsync(IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            CheckStarted();
            if (!ChangeStatus(UdpClientStatus.Connecting, s => s == UdpClientStatus.Disconnected, out UdpClientStatus oldStatus))
                throw new InvalidOperationException(
                    $"Wrong {nameof(UdpClient)} status {oldStatus}, {UdpClientStatus.Disconnected} expected");
            try
            {
                Bind(new IPEndPoint(IPAddress.Any, 0));
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
