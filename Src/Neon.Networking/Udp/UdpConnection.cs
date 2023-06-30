using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking.Udp.Channels;
using Neon.Networking.Udp.Events;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp
{
    public enum UdpConnectionStatus
    {
        InitialWaiting = 0,
        Connecting = 1,
        Connected = 2,
        Disconnecting = 3,
        Disconnected = 4
    }

    public partial class UdpConnection : IChannelConnection, IDisposable
    {
        /// <summary>
        ///     The main channel used for service messages
        /// </summary>
        public const int DEFAULT_CHANNEL = ChannelDescriptor.DEFAULT_CHANNEL;

        /// <summary>
        ///     The parent peer
        /// </summary>
        public UdpPeer Parent { get; }

        /// <summary>
        ///     Returns true if we connected state, else false
        /// </summary>
        public bool Connected => _status == UdpConnectionStatus.Connected;

        /// <summary>
        ///     Token will be cancelled as soon connection is terminated
        /// </summary>
        public CancellationToken CancellationToken => _connectionCancellationToken.Token;

        /// <summary>
        ///     Current connection MTU
        /// </summary>
        public int Mtu { get; private set; } = INITIAL_MTU;

        /// <summary>
        ///     Connection statistics
        /// </summary>
        public UdpConnectionStatistics Statistics { get; }

        /// <summary>
        ///     Current connection status
        /// </summary>
        public UdpConnectionStatus Status => _status;

        /// <summary>
        ///     Connection remote endpoint
        /// </summary>
        public EndPoint RemoteEndpoint => _udpNetEndpoint._EndPoint;

        /// <summary>
        ///     Does this connection belongs to the client
        /// </summary>
        public bool IsClientConnection { get; private set; }

        /// <summary>
        ///     A user defined tag
        /// </summary>
        public object Tag { get; set; }


        bool IsPersistent
        {
            get
            {
                if (_status == UdpConnectionStatus.Disconnected &&
                    (DateTime.UtcNow - _lastStatusChange).TotalMilliseconds >
                    Parent.Configuration.ConnectionLingerTimeout)
                    return false;
                return true;
            }
        }

        readonly ConcurrentDictionary<ChannelDescriptor, IChannel> _channels;

        readonly CancellationTokenSource _connectionCancellationToken;

        readonly object _connectionMutex = new object();
        protected readonly ILogger _logger;

        readonly IChannel _serviceReliableChannel;
        readonly IChannel _serviceUnreliableChannel;
        int? _avgLatency;

        bool _disposed;
        UdpNetEndpoint _udpNetEndpoint;
        DateTime _connectionTimeoutDeadline;
        DateTime _lastStatusChange;

        int? _latency;

        volatile UdpConnectionStatus _status;

        public UdpConnection(UdpPeer peer)
        {
            if (peer == null)
                throw new ArgumentNullException(nameof(peer));
            var rnd = new Random();
            _connectingTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _disconnectingTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _connectionCancellationToken = new CancellationTokenSource();
            Id = peer.GetNextConnectionId();
            _status = UdpConnectionStatus.InitialWaiting;
            _fragments = new ConcurrentDictionary<ushort, FragmentHolder>();
            _channels = new ConcurrentDictionary<ChannelDescriptor, IChannel>();
            _lastStatusChange = DateTime.UtcNow;
            Parent = peer;
            _logger = peer.Configuration.LogManager.GetLogger(typeof(UdpConnection));
            _logger.Meta["id"] = Id;
            Statistics = new UdpConnectionStatistics();
            _serviceReliableChannel = GetOrAddChannel(new ChannelDescriptor(0, DeliveryType.ReliableOrdered));
            _serviceUnreliableChannel = GetOrAddChannel(new ChannelDescriptor(0, DeliveryType.Unreliable));
            if (peer.Configuration is UdpConfigurationClient configurationClient)
            {
                _mtuExpand = configurationClient.AutoMtuExpand;
                _mtuExpandMaxFailAttempts = configurationClient.MtuExpandMaxFailAttempts;
                _mtuExpandFrequency = configurationClient.MtuExpandFrequency;
            }

            UpdateTimeoutDeadline();
        }

        /// <summary>
        ///     Unique connection id
        /// </summary>
        public long Id { get; }

        void IChannelConnection.ReleaseDatagram(IChannel channel, Datagram datagram)
        {
            switch (datagram.Type)
            {
                case MessageType.ConnectReq:
                    OnConnectReq(datagram);
                    break;
                case MessageType.ConnectResp:
                    OnConnectResp(datagram);
                    break;
                case MessageType.DisconnectReq:
                    OnDisconnectReq(datagram);
                    break;
                case MessageType.DisconnectResp:
                    OnDisconnectResp(datagram);
                    break;
                case MessageType.Ping:
                    OnPing(datagram);
                    break;
                case MessageType.Pong:
                    OnPong(datagram);
                    break;
                case MessageType.ExpandMTURequest:
                    OnMtuExpand(datagram);
                    break;
                case MessageType.ExpandMTUSuccess:
                    OnMtuSuccess(datagram);
                    break;
                case MessageType.UserData:
                    OnDataReceived(datagram);
                    break;
                default:
                    _logger.Warn($"#{Id} received wrong datagram {datagram.Type}");
                    break;
            }
        }

        int IChannelConnection.GetInitialResendDelay()
        {
            if (_avgLatency.HasValue)
            {
                int doubleLatency = _avgLatency.Value * 2;
                if (doubleLatency < 100)
                    doubleLatency = 100;
                return doubleLatency;
            }

            return 100;
        }

        Task IChannelConnection.SendDatagramAsync(Datagram datagram, CancellationToken cancellationToken)
        {
            if (datagram == null)
                throw new ArgumentNullException(nameof(datagram));
            Statistics.PacketOut();
            Statistics.BytesOut(datagram.GetTotalSize());
            _logger.Trace($"#{Id} sending {datagram}");
            return Parent.SendDatagramAsync(this, datagram, cancellationToken);
        }

        /// <summary>
        ///     Frees all memory associated with the connection
        /// </summary>
        public virtual void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            CloseImmediately(DisconnectReason.ClosedByThisPeer);

            foreach (IChannel channel in _channels.Values)
                channel.Dispose();

            _connectionCancellationToken.Dispose();
        }

        protected virtual void OnMessageReceived(UdpMessageInfo udpMessageInfo)
        {
        }

        protected virtual void OnStatusChanged(ConnectionStatusChangedEventArgs args)
        {
        }

        protected virtual void OnConnectionClosed(ConnectionClosedEventArgs args)
        {
        }

        protected virtual void OnConnectionOpened(ConnectionOpenedEventArgs args)
        {
        }

        void UpdateTimeoutDeadline()
        {
            _connectionTimeoutDeadline = DateTime.UtcNow.AddMilliseconds(Parent.Configuration.ConnectionTimeout);
        }

        void ReleaseMessage(UdpMessageInfo messageInfo)
        {
            _logger.Debug($"#{Id} released message {messageInfo}");
            messageInfo.Message.Position = 0;
            Parent.Configuration.SynchronizeSafe(_logger, $"{nameof(UdpConnection)}.{nameof(OnMessageReceived)}",
                state => OnMessageReceived(state as UdpMessageInfo),
                messageInfo);
        }

        internal void Init(UdpNetEndpoint udpNetEndpoint, bool isClientConnection)
        {
            _logger.Meta["endpoint"] = udpNetEndpoint._EndPoint.ToString();
            IsClientConnection = isClientConnection;
            _udpNetEndpoint = udpNetEndpoint;
            UpdateTimeoutDeadline();
            _logger.Debug($"#{Id} initialized!");
        }


        internal void OnDatagram(Datagram datagram)
        {
            if (datagram == null)
                throw new ArgumentNullException(nameof(datagram));
            if (datagram.Type != MessageType.DeliveryAck && _status == UdpConnectionStatus.Disconnected)
            {
                _logger.Trace($"#{Id} got {datagram} in closed state, drop");
                return;
            }

            Statistics.PacketIn();
            Statistics.BytesIn(datagram.GetTotalSize());

            _logger.Trace($"#{Id} received {datagram}");

            UpdateTimeoutDeadline();
            GetOrAddChannel(datagram.GetChannelDescriptor()).OnDatagram(datagram);
        }

        void OnDisconnectReq(Datagram datagram)
        {
            try
            {
                _serviceReliableChannel.SendDatagramAsync(Parent.CreateDatagramEmpty(MessageType.DisconnectResp,
                    _serviceReliableChannel.Descriptor), CancellationToken.None);
                CloseInternal(DisconnectReason.ClosedByOtherPeer);
            }
            finally
            {
                datagram.Dispose();
            }
        }

        void OnDisconnectResp(Datagram datagram)
        {
            try
            {
                UdpConnectionStatus status_ = _status;
                if (status_ == UdpConnectionStatus.Connecting)
                    CloseInternal(DisconnectReason.ClosedByOtherPeer);
                else
                    CloseInternal(DisconnectReason.ClosedByThisPeer);
            }
            finally
            {
                datagram.Dispose();
            }
        }

        void OnConnectReq(Datagram datagram)
        {
            try
            {
                if (Parent is UdpServer udpServer)
                {
                    switch (_status)
                    {
                        case UdpConnectionStatus.Connected:
                            _serviceReliableChannel.SendDatagramAsync(
                                Parent.CreateDatagramEmpty(MessageType.ConnectResp,
                                    _serviceReliableChannel.Descriptor), this.CancellationToken);
                            break;
                        case UdpConnectionStatus.InitialWaiting:

                            bool connectionAccepted = udpServer.OnAcceptConnectionInternal(
                                new OnAcceptConnectionEventArgs(_udpNetEndpoint._EndPoint));
                            ;

                            if (connectionAccepted)
                            {
                                _serviceReliableChannel.SendDatagramAsync(
                                    Parent.CreateDatagramEmpty(MessageType.ConnectResp,
                                        _serviceReliableChannel.Descriptor), this.CancellationToken);
                                EndConnect();
                            }
                            else
                            {
                                _serviceReliableChannel.SendDatagramAsync(
                                    Parent.CreateDatagramEmpty(MessageType.DisconnectResp,
                                        _serviceReliableChannel.Descriptor), this.CancellationToken);
                                CloseInternal(DisconnectReason.ClosedByThisPeer);
                            }

                            break;
                        case UdpConnectionStatus.Disconnected:
                            _serviceReliableChannel.SendDatagramAsync(
                                Parent.CreateDatagramEmpty(MessageType.DisconnectResp,
                                    _serviceReliableChannel.Descriptor), this.CancellationToken);
                            break;
                    }
                }
                else
                {
                    _serviceReliableChannel.SendDatagramAsync(Parent.CreateDatagramEmpty(MessageType.DisconnectResp,
                        _serviceReliableChannel.Descriptor), this.CancellationToken);
                    CloseInternal(DisconnectReason.ClosedByOtherPeer);
                }
            }
            finally
            {
                datagram.Dispose();
            }
        }

        void OnConnectResp(Datagram datagram)
        {
            try
            {
                if (Parent is UdpServer)
                    return;
                EndConnect();
            }
            finally
            {
                datagram.Dispose();
            }
        }

        void OnDataReceived(Datagram datagram)
        {
            if (!CheckStatusForDatagram(datagram, UdpConnectionStatus.Connected))
            {
                datagram.Dispose();
                return;
            }

            if (datagram.IsFragmented)
                ManageFragment(datagram);
            else
                try
                {
                    ReleaseMessage(Parent.ConvertDatagramToUdpRawMessage(datagram));
                }
                finally
                {
                    datagram.Dispose();
                }
        }

        IChannel GetOrAddChannel(ChannelDescriptor descriptor)
        {
            if (descriptor.DeliveryType == DeliveryType.Unreliable && descriptor.Channel != 0)
                descriptor =
                    new ChannelDescriptor(0,
                        DeliveryType.Unreliable); //no need to maintain channels for unreliable datagrams

            IChannel result = _channels.GetOrAdd(descriptor, desc =>
            {
                switch (desc.DeliveryType)
                {
                    case DeliveryType.ReliableOrdered:
                        return new ReliableChannel(Parent.Configuration.LogManager, desc, this, true);
                    case DeliveryType.ReliableUnordered:
                        return new ReliableChannel(Parent.Configuration.LogManager, desc, this, false);
                    case DeliveryType.Unreliable:
                        return new UnreliableChannel(Parent.Configuration.LogManager, desc, this);
                    case DeliveryType.UnreliableSequenced:
                        return new UnreliableSequencedChannel(Parent.Configuration.LogManager, desc, this);
                    default:
                        throw new ArgumentException("Got datagram with unknown delivery type");
                }
            });

            return result;
        }

        protected virtual void PollEvents()
        {
            
        }
        
        internal bool PollEventsInternal()
        {
            Statistics.PollEvents();

            UdpConnectionStatus status_ = _status;
            if (status_ == UdpConnectionStatus.Connected)
                PollEvents();

            if (status_ == UdpConnectionStatus.Disconnected)
                return IsPersistent;

            if (status_ == UdpConnectionStatus.Connected)
            {
                TrySendPing();
                MtuCheck();
            }

            foreach (KeyValuePair<ChannelDescriptor, IChannel> pair in _channels)
                pair.Value.PollEvents();

            if (DateTime.UtcNow >= _connectionTimeoutDeadline) CloseInternal(DisconnectReason.Timeout);

            return true;
        }

        /// <summary>
        ///     Sends the message
        /// </summary>
        /// <param name="udpMessageInfo">A message</param>
        /// <exception cref="IOException">If connection not in the Connected state</exception>
        /// <exception cref="ArgumentException">If the message too big to be send with this channel</exception>
        public async Task SendMessageAsync(UdpMessageInfo udpMessageInfo, CancellationToken cancellationToken)
        {
            if (_status != UdpConnectionStatus.Connected)
                throw new IOException("Connection not established");

            var descriptor = new ChannelDescriptor(udpMessageInfo.Channel, udpMessageInfo.DeliveryType);
            IChannel channel_ = GetOrAddChannel(descriptor);

            if (!CheckCanBeSendUnfragmented(udpMessageInfo.Message))
            {
                //need split
                if (udpMessageInfo.DeliveryType == DeliveryType.Unreliable ||
                    udpMessageInfo.DeliveryType == DeliveryType.UnreliableSequenced)
                {
                    if (Parent.Configuration.TooLargeUnreliableMessageBehaviour ==
                        UdpConfigurationPeer.TooLargeMessageBehaviour.RaiseException)
                        throw new ArgumentException(
                            "Message too big. You couldn't send fragmented message through unreliable channel. Make a message below MTU limit or change delivery type");
                    return;
                }

                await SendFragmentedMessage(udpMessageInfo.Message, channel_, cancellationToken);
                return;
            }

            Datagram datagram =
                Parent.ConvertMessageToDatagram(MessageType.UserData, channel_.Descriptor, udpMessageInfo);
            await channel_.SendDatagramAsync(datagram, cancellationToken);
            _logger.Debug($"#{Id} sent {udpMessageInfo}");
            udpMessageInfo.Message.Dispose();
        }

        public override string ToString()
        {
            return $"{nameof(UdpConnection)}[id={Id},endpoint={_udpNetEndpoint._EndPoint}]";
        }
    }
}