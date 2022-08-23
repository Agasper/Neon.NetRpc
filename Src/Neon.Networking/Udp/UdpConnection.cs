﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Neon.Networking.Udp.Channels;
using Neon.Networking.Udp.Events;
using Neon.Networking.Udp.Messages;
using Neon.Logging;

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
        public const int DEFAULT_CHANNEL = ChannelDescriptor.DEFAULT_CHANNEL;
        
        public UdpPeer Parent => peer;
        public bool Connected => status == UdpConnectionStatus.Connected; 
        public CancellationToken CancellationToken => connectionCancellationToken.Token;
        public long Id { get; }
        public int Mtu { get; private set; } = INITIAL_MTU;
        public UdpConnectionStatistics Statistics { get; private set; }
        public UdpConnectionStatus Status => status;
        public UdpNetEndpoint EndPoint { get; private set; }
        public bool IsClientConnection { get; private set; }
        public object Tag { get; set; }

        UdpPeer peer;
        protected readonly ILogger logger;
        ConcurrentDictionary<ChannelDescriptor, IChannel> channels;

        volatile UdpConnectionStatus status;
        DateTime lastStatusChange;
        readonly CancellationTokenSource connectionCancellationToken;

        IChannel serviceReliableChannel;
        IChannel serviceUnreliableChannel;

        DateTime connectionTimeoutDeadline;

        int? latency;
        int? avgLatency;

        object connectionMutex = new object();

        public UdpConnection(UdpPeer peer)
        {
            if (peer == null)
                throw new ArgumentNullException(nameof(peer));
            Random rnd = new Random();
            this.connectingTcs = new TaskCompletionSource<object>();
            this.disconnectingTcs = new TaskCompletionSource<object>();
            this.connectionCancellationToken = new CancellationTokenSource();
            this.Id = peer.GetNextConnectionId();
            this.status = UdpConnectionStatus.InitialWaiting;
            this.fragments = new ConcurrentDictionary<ushort, FragmentHolder>();
            this.channels = new ConcurrentDictionary<ChannelDescriptor, IChannel>();
            this.lastStatusChange = DateTime.UtcNow;
            this.peer = peer;
            this.logger = peer.Configuration.LogManager.GetLogger(nameof(UdpConnection));
            this.logger.Meta["kind"] = this.GetType().Name;
            this.logger.Meta["id"] = this.Id;
            this.Statistics = new UdpConnectionStatistics();
            this.serviceReliableChannel = GetOrAddChannel(new ChannelDescriptor(0, DeliveryType.ReliableOrdered));
            this.serviceUnreliableChannel = GetOrAddChannel(new ChannelDescriptor(0, DeliveryType.Unreliable));
            this.UpdateTimeoutDeadline();
        }

        public virtual void Dispose()
        {
            CloseImmediately(DisconnectReason.ClosedByThisPeer);

            foreach (var channel in channels.Values)
                channel.Dispose();
            
            connectionCancellationToken.Dispose();
        }

        protected virtual void OnMessageReceived(UdpRawMessage udpRawMessage)
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
            this.connectionTimeoutDeadline = DateTime.UtcNow.AddMilliseconds(peer.Configuration.ConnectionTimeout);
        }

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
                    logger.Warn($"#{Id} received wrong datagram {datagram.Type}");
                    break;
            }
        }

        void ReleaseMessage(UdpRawMessage message)
        {
            logger.Debug($"#{Id} released message {message}");
            message.Message.Position = 0;
            peer.Configuration.SynchronizeSafe(logger, $"{nameof(UdpConnection)}.{nameof(OnMessageReceived)}",
                (state) => OnMessageReceived(state as UdpRawMessage),
                message);
        }

        internal void Init(UdpNetEndpoint udpNetEndpoint, bool isClientConnection)
        {
            this.logger.Meta["endpoint"] = udpNetEndpoint.EndPoint.ToString();
            this.IsClientConnection = isClientConnection;
            this.EndPoint = udpNetEndpoint;
            UpdateTimeoutDeadline();
            logger.Debug($"#{Id} initialized!");
        }
        

        internal void OnDatagram(Datagram datagram)
        {
            if (datagram == null)
                throw new ArgumentNullException(nameof(datagram));
            
            Statistics.PacketIn();
            Statistics.BytesIn(datagram.GetTotalSize());
            
            logger.Trace($"#{Id} received {datagram}");
            
            UpdateTimeoutDeadline();
            GetOrAddChannel(datagram.GetChannelDescriptor()).OnDatagram(datagram);
        }

        void OnDisconnectReq(Datagram datagram)
        {
            try
            {
                serviceReliableChannel.SendDatagramAsync(Parent.CreateDatagramEmpty(MessageType.DisconnectResp, serviceReliableChannel.Descriptor));
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
                var status_ = this.status;
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
                if (this.peer is UdpServer udpServer)
                {
                    switch (status)
                    {
                        case UdpConnectionStatus.Connected:
                            serviceReliableChannel.SendDatagramAsync(Parent.CreateDatagramEmpty(MessageType.ConnectResp, serviceReliableChannel.Descriptor));
                            break;
                        case UdpConnectionStatus.InitialWaiting:

                            bool connectionAccepted = udpServer.OnAcceptConnectionInternal(
                                new OnAcceptConnectionEventArgs(this.EndPoint.EndPoint));;

                            if (connectionAccepted)
                            {
                                serviceReliableChannel.SendDatagramAsync(Parent.CreateDatagramEmpty(MessageType.ConnectResp, serviceReliableChannel.Descriptor));
                                EndConnect();
                            }
                            else
                            {
                                serviceReliableChannel.SendDatagramAsync(Parent.CreateDatagramEmpty(MessageType.DisconnectResp, serviceReliableChannel.Descriptor));
                                CloseInternal(DisconnectReason.ClosedByThisPeer);
                            }

                            break;
                        case UdpConnectionStatus.Disconnected:
                            serviceReliableChannel.SendDatagramAsync(Parent.CreateDatagramEmpty(MessageType.DisconnectResp, serviceReliableChannel.Descriptor));
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    serviceReliableChannel.SendDatagramAsync(Parent.CreateDatagramEmpty(MessageType.DisconnectResp, serviceReliableChannel.Descriptor));
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
                if (this.peer is UdpServer)
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
            {
                try
                {
                    ReleaseMessage(Parent.ConvertDatagramToUdpRawMessage(datagram));
                }
                finally
                {
                    datagram.Dispose();
                }
            }
        }

        IChannel GetOrAddChannel(ChannelDescriptor descriptor)
        {
            if (descriptor.DeliveryType == DeliveryType.Unreliable && descriptor.Channel != 0)
                descriptor = new ChannelDescriptor(0, DeliveryType.Unreliable); //no need to maintain channels for unreliable datagrams
            
            IChannel result = channels.GetOrAdd(descriptor, (desc) => {
                switch (desc.DeliveryType)
                {
                    case DeliveryType.ReliableOrdered:
                        return new ReliableChannel(peer.Configuration.LogManager, desc, this, true);
                    case DeliveryType.ReliableUnordered:
                        return new ReliableChannel(peer.Configuration.LogManager, desc, this, false);
                    case DeliveryType.Unreliable:
                        return new UnreliableChannel(peer.Configuration.LogManager, desc, this);
                    case DeliveryType.UnreliableSequenced:
                        return new UnreliableSequencedChannel(peer.Configuration.LogManager, desc, this);
                    default:
                        throw new ArgumentException("Got datagram with unknown delivery type");
                }
            });

            return result;
        }
        
        
        bool IsPersistent
        {
            get
            {
                if (status == UdpConnectionStatus.Disconnected &&
                    (DateTime.UtcNow - lastStatusChange).TotalMilliseconds > peer.Configuration.ConnectionLingerTimeout)
                    return false;
                return true;
            }
        }

        internal bool PollEvents()
        {
            Statistics.PollEvents();

            var status_ = this.status;

            if (status_ == UdpConnectionStatus.Disconnected)
                return IsPersistent;

            if (status_ == UdpConnectionStatus.Connected)
            {
                TrySendPing();
                MtuCheck();
            }

            foreach (var pair in channels)
                pair.Value.PollEvents();

            if (DateTime.UtcNow >= this.connectionTimeoutDeadline)
            {
                CloseInternal(DisconnectReason.Timeout);
            }

            return true;
        }

        int IChannelConnection.GetInitialResendDelay()
        {
            if (latency.HasValue)
            {
                int doubleLatency = (int)(avgLatency.Value * 2);
                if (doubleLatency < 100)
                    doubleLatency = 100;
                return doubleLatency;
            }
            else
            {
                return 100;
            }
        }

        Task IChannelConnection.SendDatagramAsync(Datagram datagram)
        {
            if (datagram == null) 
                throw new ArgumentNullException(nameof(datagram));
            Statistics.PacketOut();
            Statistics.BytesOut(datagram.GetTotalSize());
            logger.Trace($"#{Id} sending {datagram}");
            return peer.SendDatagramAsync(this, datagram);
        }

        public Task SendMessageAsync(UdpRawMessage udpRawMessage)
        {
            if (this.status != UdpConnectionStatus.Connected)
                throw new IOException("Connection not established");

            var descriptor = new ChannelDescriptor(udpRawMessage.Channel, udpRawMessage.DeliveryType);
            IChannel channel_ = GetOrAddChannel(descriptor);

            if (!CheckCanBeSendUnfragmented(udpRawMessage.Message))
            { 
                //need split
                if (udpRawMessage.DeliveryType == DeliveryType.Unreliable || udpRawMessage.DeliveryType == DeliveryType.UnreliableSequenced)
                {
                    if (peer.Configuration.TooLargeUnreliableMessageBehaviour == UdpConfigurationPeer.TooLargeMessageBehaviour.RaiseException)
                        throw new ArgumentException("Message too big. You couldn't send fragmented message through unreliable channel. Make a message below MTU limit or change delivery type");
                    else
                        return Task.CompletedTask;
                }

                return SendFragmentedMessage(udpRawMessage.Message, channel_);
            }

            Datagram datagram = Parent.ConvertMessageToDatagram(MessageType.UserData, channel_.Descriptor, udpRawMessage);
            var status = channel_.SendDatagramAsync(datagram);
            udpRawMessage.Message.Dispose();

            return status;
        }

        public override string ToString()
        {
            return $"{nameof(UdpConnection)}[id={Id},endpoint={EndPoint}]";
        }
    }
}