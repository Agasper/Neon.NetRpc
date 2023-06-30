using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking.Messages;
using Neon.Networking.Udp.Events;
using Neon.Networking.Udp.Messages;
using Neon.Util.Polling;
using Neon.Util.Pooling.Objects;

namespace Neon.Networking.Udp
{
    public abstract class UdpPeer
    {
        public virtual object Tag { get; set; }
        public bool IsStarted => _poller.IsStarted;
        public UdpConfigurationPeer Configuration { get; }

        private protected abstract ILogger Logger { get; }
        private protected bool IsBound => _socket != null && _socket.IsBound;
        readonly Poller _poller;
        readonly Random _random;
        readonly GenericPool<SocketAsyncEventArgs> _socketArgsPool;
        long _connectionId;

        IPEndPoint _lastBoundTo;
        Socket _socket;

        public UdpPeer(UdpConfigurationPeer configuration)
        {
            _socketArgsPool = new GenericPool<SocketAsyncEventArgs>(() =>
            {
                var arg = new SocketAsyncEventArgs();
                arg.SetBuffer(new byte[ushort.MaxValue], 0, ushort.MaxValue);
                Socket socket_ = _socket;
                if (socket_ == null)
                    throw new ObjectDisposedException(nameof(_socket));
                if (socket_.LocalEndPoint.AddressFamily == AddressFamily.InterNetwork)
                    arg.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                else if (socket_.LocalEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
                    arg.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                else
                    throw new InvalidOperationException(
                        $"Invalid socket address family: {socket_.LocalEndPoint.AddressFamily}");
                arg.Completed += IO_Complete;
                return arg;
            }, 0);

            _random = new Random();
            _poller = new Poller(5, PollEventsInternal_);

            Configuration = configuration;
            Configuration.Lock();
        }

        internal virtual void OnConnectionClosedInternal(ConnectionClosedEventArgs args)
        {
            Configuration.SynchronizeSafe(Logger, $"{nameof(UdpPeer)}.{nameof(OnConnectionClosed)}",
                state => OnConnectionClosed(state as ConnectionClosedEventArgs), args);
        }

        internal virtual void OnConnectionOpenedInternal(ConnectionOpenedEventArgs args)
        {
            Configuration.SynchronizeSafe(Logger, $"{nameof(UdpPeer)}.{nameof(OnConnectionOpened)}",
                state => OnConnectionOpened(state as ConnectionOpenedEventArgs), args);
        }

        internal virtual void OnConnectionStatusChangedInternal(ConnectionStatusChangedEventArgs args)
        {
            Configuration.SynchronizeSafe(Logger, $"{nameof(UdpPeer)}.{nameof(OnConnectionStatusChanged)}",
                state => OnConnectionStatusChanged(state as ConnectionStatusChangedEventArgs), args);
        }

        protected virtual void OnConnectionClosed(ConnectionClosedEventArgs args)
        {
        }

        protected virtual void OnConnectionOpened(ConnectionOpenedEventArgs args)
        {
        }

        protected virtual void OnConnectionStatusChanged(ConnectionStatusChangedEventArgs args)
        {
        }

        internal long GetNextConnectionId()
        {
            return Interlocked.Increment(ref _connectionId);
        }

        void IO_Complete(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                if (e.LastOperation == SocketAsyncOperation.ReceiveFrom)
                    EndReceive(e);
                if (e.LastOperation == SocketAsyncOperation.SendTo)
                    EndSend(e);
            }
            finally
            {
                _socketArgsPool.Return(e);
            }
        }

        /// <summary>
        ///     Start an internal network thread
        /// </summary>
        public virtual void Start()
        {
            _poller.StartPolling();
        }

        /// <summary>
        ///     Disconnects and shutdown the client
        /// </summary>
        public virtual void Shutdown()
        {
            _socketArgsPool.Clear();
            _poller.StopPolling(false);
            DestroySocket();
        }


        protected void CheckStarted()
        {
            if (!IsStarted)
                throw new InvalidOperationException("Please call Start() first");
        }

        public RawMessage CreateMessage()
        {
            return new RawMessage(Configuration.MemoryManager);
        }

        public RawMessage CreateMessage(ArraySegment<byte> segment)
        {
            return new RawMessage(Configuration.MemoryManager, segment);
        }

        public RawMessage CreateMessage(int length)
        {
            return new RawMessage(Configuration.MemoryManager, length);
        }

        internal RawMessage CreateMessage(int length, bool compressed, bool encrypted)
        {
            return new RawMessage(Configuration.MemoryManager, length, compressed, encrypted);
        }

        internal Datagram CreateDatagram(ChannelDescriptor channelDescriptor)
        {
            var datagram = new Datagram(Configuration.MemoryManager);
            datagram.UpdateForChannelDescriptor(channelDescriptor);
            return datagram;
        }

        internal Datagram CreateDatagram(ArraySegment<byte> segment, ChannelDescriptor channelDescriptor)
        {
            var newGuid = Guid.NewGuid();
            var datagram = new Datagram(Configuration.MemoryManager, segment);
            datagram.UpdateForChannelDescriptor(channelDescriptor);
            return datagram;
        }

        internal Datagram CreateDatagram(int payloadSize, ChannelDescriptor channelDescriptor)
        {
            var newGuid = Guid.NewGuid();
            var datagram = new Datagram(Configuration.MemoryManager, payloadSize);
            datagram.UpdateForChannelDescriptor(channelDescriptor);
            return datagram;
        }

        internal Datagram CreateDatagramEmpty(MessageType messageType, ChannelDescriptor channelDescriptor)
        {
            var newGuid = Guid.NewGuid();
            var datagram = new Datagram(Configuration.MemoryManager);
            datagram.UpdateForChannelDescriptor(channelDescriptor);
            datagram.Type = messageType;
            return datagram;
        }

        internal Datagram CreateDatagram(MessageType messageType, ChannelDescriptor channelDescriptor, int payloadSize)
        {
            var newGuid = Guid.NewGuid();
            var datagram = new Datagram(Configuration.MemoryManager, payloadSize);
            datagram.UpdateForChannelDescriptor(channelDescriptor);
            datagram.Type = messageType;
            return datagram;
        }

        internal Datagram CreateDatagram(MessageType messageType, ChannelDescriptor channelDescriptor,
            RawMessage payload)
        {
            var newGuid = Guid.NewGuid();
            var datagram = new Datagram(Configuration.MemoryManager, payload.Length);
            datagram.Type = messageType;
            datagram.UpdateForChannelDescriptor(channelDescriptor);
            payload.Position = 0;
            payload.CopyTo(datagram);
            return datagram;
        }

        internal Datagram ConvertMessageToDatagram(MessageType messageType, ChannelDescriptor channelDescriptor,
            UdpMessageInfo messageInfo)
        {
            Datagram datagram = ConvertMessageToDatagram(messageType, channelDescriptor, messageInfo.Message);
            datagram.Channel = messageInfo.Channel;
            datagram.DeliveryType = messageInfo.DeliveryType;
            return datagram;
        }

        internal Datagram ConvertMessageToDatagram(MessageType messageType, ChannelDescriptor channelDescriptor,
            IRawMessage message)
        {
            Datagram datagram = null;
            if (message.Length == 0)
            {
                datagram = CreateDatagramEmpty(messageType, channelDescriptor);
            }
            else
            {
                datagram = CreateDatagram(messageType, channelDescriptor, message.Length);
                message.Position = 0;
                message.CopyTo(datagram);
                datagram.Position = 0;
            }

            datagram.Compressed = message.Compressed;
            datagram.Encrypted = message.Encrypted;
            datagram.Type = messageType;

            return datagram;
        }

        internal UdpMessageInfo ConvertDatagramToUdpRawMessage(Datagram datagram)
        {
            RawMessage message = ConvertDatagramToRawMessage(datagram);
            return new UdpMessageInfo(message, datagram.DeliveryType, datagram.Channel);
        }

        internal RawMessage ConvertDatagramToRawMessage(Datagram datagram)
        {
            RawMessage message = CreateMessage(datagram.Length, datagram.Compressed, datagram.Encrypted);
            datagram.Position = 0;
            datagram.CopyTo(message);
            message.Position = 0;
            return message;
        }

        void PollEventsInternal_()
        {
            try
            {
                PollEventsInternal();
                PollEvents();
            }
            catch (Exception ex)
            {
                Logger.Critical($"Exception in polling thread: {ex}");
                Shutdown();
            }
        }

        protected virtual void PollEvents()
        {
        }

        private protected virtual void PollEventsInternal()
        {
        }

        protected virtual UdpConnection CreateConnection()
        {
            return new UdpConnection(this);
        }

        void Rebind()
        {
            if (_lastBoundTo == null)
                throw new InvalidOperationException("Rebind failed: socket never have been bound");
            Bind(_lastBoundTo);
        }

        protected void DestroySocket()
        {
            Socket socket_ = _socket;
            if (socket_ != null)
            {
                socket_.Close();
                _socket = null;
            }
        }

        protected virtual void Bind(int port)
        {
            Bind(null, port);
        }

        protected virtual void Bind(string host, int port)
        {
            IPEndPoint myEndpoint;
            if (string.IsNullOrEmpty(host))
                myEndpoint = new IPEndPoint(IPAddress.Any, port);
            else
                myEndpoint = new IPEndPoint(IPAddress.Parse(host), port);

            Bind(myEndpoint);
        }

        protected virtual void Bind(IPEndPoint endPoint)
        {
            CheckStarted();
            if (_socket != null)
                throw new InvalidOperationException("Socket already bound to this peer");

            _socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.ExclusiveAddressUse = !Configuration.ReuseAddress;
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress,
                Configuration.ReuseAddress);
            _socket.Blocking = false;

            _socket.ReceiveBufferSize = Configuration.ReceiveBufferSize;
            _socket.SendBufferSize = Configuration.SendBufferSize;

            _socket.Bind(endPoint);
            _lastBoundTo = endPoint;

            try
            {
                const uint IOC_IN = 0x80000000;
                const uint IOC_VENDOR = 0x18000000;
                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                _socket.IOControl((int) SIO_UDP_CONNRESET, new[] {Convert.ToByte(false)}, null);
            }
            catch
            {
                Logger.Debug("SIO_UDP_CONNRESET not supported on this platform");
                // ignore; SIO_UDP_CONNRESET not supported on this platform
            }

            try
            {
                _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DontFragment, 1);
            }
            catch
            {
                Logger.Debug("DONT_FRAGMENT not supported on this platform");
            }

            for (var i = 0; i < Configuration.NetworkReceiveThreads; i++)
                StartReceive();
        }

        void StartReceive()
        {
            try
            {
                Socket socket_ = _socket;
                if (socket_ == null)
                    return;
                SocketAsyncEventArgs arg = _socketArgsPool.Pop();
                arg.SetBuffer(arg.Buffer, 0, arg.Buffer.Length);
                if (!socket_.ReceiveFromAsync(arg))
                    IO_Complete(this, arg);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        void EndReceive(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError != SocketError.Success)
                    switch (e.SocketError)
                    {
                        case SocketError.ConnectionReset:
                            // connection reset by peer, aka connection forcibly closed aka "ICMP port unreachable"
                            // we should shut down the connection; but sender seemingly cannot be trusted, so which connection should we shut down?!
                            // So, what to do?
                            return;

                        case SocketError.NotConnected:
                            Logger.Debug("Socket has been unbound. Rebinding");
                            // socket is unbound; try to rebind it (happens on mobile when process goes to sleep)
                            Rebind();
                            return;

                        case SocketError.OperationAborted:
                            //Socket was closed
                            return;

                        default:
                            Logger.Error("Socket error on receive: " + e.SocketError);
                            return;
                    }

                if (Configuration.ConnectionSimulation != null &&
                    _random.NextDouble() < Configuration.ConnectionSimulation.PacketLoss)
                {
                    Logger.Debug(
                        $"We got a datagram from {e.RemoteEndPoint}, but according to connection simulation rules we dropped it");
                    return;
                }

                var segment = new ArraySegment<byte>(e.Buffer, 0, e.BytesTransferred);
                //Logger.Trace($"Got datagram {segment.Count} bytes from {e.RemoteEndPoint}");
                Datagram datagram = Datagram.Parse(Configuration.MemoryManager, segment);
                var ep = new UdpNetEndpoint(e.RemoteEndPoint);

                if (Configuration.ConnectionSimulation != null)
                    OnDatagramInternal(datagram, ep);
                else
                    _ = OnDatagramInternalWithSimulationAsync(datagram, ep);
            }
            catch (Exception ex)
            {
                Logger.Error($"Unhandled exception in EndReceive: {ex}");
            }
            finally
            {
                StartReceive();
            }
        }

        void OnDatagramInternal(Datagram datagram, UdpNetEndpoint remoteEndpoint)
        {
            try
            {
                OnDatagram(datagram, remoteEndpoint);
            }
            catch (Exception ex)
            {
                Logger.Error($"Unhandled exception on {datagram} from {remoteEndpoint} processing: {ex}");
            }
        }

        async Task OnDatagramInternalWithSimulationAsync(Datagram datagram, UdpNetEndpoint remoteEndpoint)
        {
            if (Configuration.ConnectionSimulation != null)
            {
                int delay = Configuration.ConnectionSimulation.GetHalfDelay();
                if (delay > 0)
                    await Task.Delay(delay).ConfigureAwait(false);
            }

            try
            {
                OnDatagram(datagram, remoteEndpoint);
            }
            catch (Exception ex)
            {
                Logger.Error($"Unhandled exception on {datagram} from {remoteEndpoint} processing: {ex}");
            }
        }

        private protected abstract void OnDatagram(Datagram datagram, UdpNetEndpoint remoteEndpoint);

        internal async Task SendDatagramAsync(UdpConnection connection, Datagram datagram, CancellationToken cancellationToken)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (datagram == null)
                throw new ArgumentNullException(nameof(datagram));

            if (Configuration.ConnectionSimulation != null)
            {
                if (_random.NextDouble() < Configuration.ConnectionSimulation.PacketLoss)
                {
                    Logger.Trace(
                        $"We're sending {datagram} for {connection}, but according to connection simulation rules we dropped it");
                    return;
                }

                int delay = Configuration.ConnectionSimulation.GetHalfDelay();
                if (delay > 0)
                    await Task.Delay(delay).ConfigureAwait(false);
            }

            Socket socket_ = _socket;
            if (socket_ == null)
                throw new ObjectDisposedException(nameof(_socket), "Socket was disposed");

            var operationInfo = new SendDatagramAsyncResult(connection, datagram);
            SocketAsyncEventArgs arg = _socketArgsPool.Pop();
            int bytes = datagram.BuildTo(new ArraySegment<byte>(arg.Buffer, 0, arg.Buffer.Length));
            arg.SetBuffer(arg.Buffer, 0, bytes);
            arg.RemoteEndPoint = connection.RemoteEndpoint;
            arg.UserToken = operationInfo;
            if (!socket_.SendToAsync(arg))
                IO_Complete(this, arg);
            SocketError result = await operationInfo.Task.ConfigureAwait(false);
            if (result != SocketError.Success)
                throw new SocketException((int) result);
        }

        void EndSend(SocketAsyncEventArgs e)
        {
            try
            {
                var opInfo = (SendDatagramAsyncResult) e.UserToken;
                opInfo.SetComplete(e.SocketError);
                //
                // if (e.SocketError != SocketError.Success)
                // {
                //     switch (e.SocketError)
                //     {
                //         case SocketError.WouldBlock:
                //             // send buffer full?
                //             //    LogWarning("Socket threw exception; would block - send buffer full?");
                //             Logger.Error("Send error SocketError.WouldBlock: probably buffer is full.");
                //             break;
                //
                //         case SocketError.ConnectionReset:
                //             Logger.Debug($"Remote peer responded with connection reset");
                //             opInfo.Connection.CloseImmediately(DisconnectReason.ClosedByOtherPeer);
                //             // connection reset by peer, aka connection forcibly closed aka "ICMP port unreachable" 
                //             break;
                //         default:
                //             Logger.Error($"Sending {opInfo.Datagram} failed: {e.SocketError}");
                //             break;
                //     }
                // }
            }
            finally
            {
                e.UserToken = null;
            }
        }
    }
}