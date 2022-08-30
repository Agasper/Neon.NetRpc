using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Neon.Networking.Udp.Events;
using Neon.Networking.Udp.Messages;
using Neon.Logging;
using Neon.Networking.Messages;
using Neon.Util.Polling;
using Neon.Util.Pooling.Objects;

namespace Neon.Networking.Udp
{
    public abstract class UdpPeer
    {
        public virtual object Tag { get; set; }
        public bool IsStarted => poller.IsStarted;
        public UdpConfigurationPeer Configuration => configuration;

        private protected abstract ILogger Logger { get; }
        private protected bool IsBound => socket != null && socket.IsBound;

        readonly UdpConfigurationPeer configuration;
        readonly Poller poller;
        readonly GenericPool<SocketAsyncEventArgs> socketArgsPool;
        readonly Random random;
        
        IPEndPoint lastBoundTo;
        Socket socket;
        long connectionId;

        public UdpPeer(UdpConfigurationPeer configuration)
        {
            this.socketArgsPool = new GenericPool<SocketAsyncEventArgs>(() =>
            {
                var arg = new SocketAsyncEventArgs();
                arg.SetBuffer(new byte[ushort.MaxValue], 0, ushort.MaxValue);
                if (socket.LocalEndPoint.AddressFamily == AddressFamily.InterNetwork)
                    arg.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                else if (socket.LocalEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
                    arg.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                else
                    throw new InvalidOperationException(
                    $"Invalid socket address family: {socket.LocalEndPoint.AddressFamily}");
                arg.Completed += IO_Complete;
                return arg;
            }, 0);

            this.random = new Random();
            this.poller = new Poller(5, PollEventsInternal_);
            
            this.configuration = configuration;
            this.configuration.Lock();
        }

        internal virtual void OnConnectionClosedInternal(ConnectionClosedEventArgs args)
        {
            configuration.SynchronizeSafe(Logger, $"{nameof(UdpPeer)}.{nameof(OnConnectionClosed)}",
                (state) => OnConnectionClosed(state as ConnectionClosedEventArgs), args);
        }
        
        internal virtual void OnConnectionOpenedInternal(ConnectionOpenedEventArgs args)
        {
            configuration.SynchronizeSafe(Logger, $"{nameof(UdpPeer)}.{nameof(OnConnectionOpened)}",
                (state) => OnConnectionOpened(state as ConnectionOpenedEventArgs), args);
        }
        
        internal virtual void OnConnectionStatusChangedInternal(ConnectionStatusChangedEventArgs args)
        {
            configuration.SynchronizeSafe(Logger, $"{nameof(UdpPeer)}.{nameof(OnConnectionStatusChanged)}",
                (state) => OnConnectionStatusChanged(state as ConnectionStatusChangedEventArgs), args);
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
            return Interlocked.Increment(ref connectionId);
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
                socketArgsPool.Return(e);
            }
        }

        /// <summary>
        /// Start an internal network thread
        /// </summary>
        public virtual void Start()
        {
            this.poller.StartPolling();
        }

        /// <summary>
        /// Disconnects and shutdown the client
        /// </summary>
        public virtual void Shutdown()
        {
            this.socketArgsPool.Clear();
            this.poller.StopPolling(false);
            DestroySocket();
        }


        protected void CheckStarted()
        {
            if (!IsStarted)
                throw new InvalidOperationException("Please call Start() first");
        }

        public RawMessage CreateMessage()
        {
            Guid newGuid = Guid.NewGuid();
            return new RawMessage(configuration.MemoryManager, configuration.MemoryManager.GetStream(newGuid), false, false, newGuid);
        }

        public RawMessage CreateMessage(ArraySegment<byte> segment)
        {
            Guid newGuid = Guid.NewGuid();
            return new RawMessage(configuration.MemoryManager, configuration.MemoryManager.GetStream(segment, newGuid), false, false, newGuid);
        }

        public RawMessage CreateMessage(int length)
        {
            Guid newGuid = Guid.NewGuid();
            return new RawMessage(configuration.MemoryManager, configuration.MemoryManager.GetStream(length, newGuid), false, false, newGuid);
        }
        
        internal RawMessage CreateMessage(int length, bool compressed, bool encrypted)
        {
            Guid newGuid = Guid.NewGuid();
            return new RawMessage(configuration.MemoryManager, configuration.MemoryManager.GetStream(length, newGuid), compressed, encrypted, newGuid);
        }
        
        internal RawMessage CreateMessageEmpty()
        {
            Guid newGuid = Guid.NewGuid();
            return new RawMessage(configuration.MemoryManager, null, false, false, newGuid);
        }
        
        internal Datagram CreateDatagram(ChannelDescriptor channelDescriptor)
        {
            Guid newGuid = Guid.NewGuid();
            var datagram = new Datagram(configuration.MemoryManager, configuration.MemoryManager.GetStream(newGuid), newGuid);
            datagram.UpdateForChannelDescriptor(channelDescriptor);
            return datagram;
        }

        internal Datagram CreateDatagram(ArraySegment<byte> segment, ChannelDescriptor channelDescriptor)
        {
            Guid newGuid = Guid.NewGuid();
            var datagram = new Datagram(configuration.MemoryManager, configuration.MemoryManager.GetStream(segment, newGuid), newGuid);
            datagram.UpdateForChannelDescriptor(channelDescriptor);
            return datagram;
        }

        internal Datagram CreateDatagram(int payloadSize, ChannelDescriptor channelDescriptor)
        {
            Guid newGuid = Guid.NewGuid();
            var datagram = new Datagram(configuration.MemoryManager, configuration.MemoryManager.GetStream(payloadSize, newGuid), newGuid);
            datagram.UpdateForChannelDescriptor(channelDescriptor);
            return datagram;
        }
        
        internal Datagram CreateDatagramEmpty(MessageType messageType, ChannelDescriptor channelDescriptor)
        {
            Guid newGuid = Guid.NewGuid();
            var datagram = new Datagram(configuration.MemoryManager, null, newGuid);
            datagram.UpdateForChannelDescriptor(channelDescriptor);
            datagram.Type = messageType;
            return datagram;
        }

        internal Datagram CreateDatagram(MessageType messageType, ChannelDescriptor channelDescriptor, int payloadSize)
        {
            Guid newGuid = Guid.NewGuid();
            var datagram = new Datagram(configuration.MemoryManager, configuration.MemoryManager.GetStream(payloadSize, newGuid), newGuid);
            datagram.UpdateForChannelDescriptor(channelDescriptor);
            datagram.Type = messageType;
            return datagram;
        }

        internal Datagram CreateDatagram(MessageType messageType, ChannelDescriptor channelDescriptor, RawMessage payload)
        {
            Guid newGuid = Guid.NewGuid();
            var datagram = new Datagram(configuration.MemoryManager, configuration.MemoryManager.GetStream(payload.Length, newGuid), newGuid);
            datagram.Type = messageType;
            datagram.UpdateForChannelDescriptor(channelDescriptor);
            payload.Position = 0;
            payload.CopyTo(datagram);
            return datagram;
        }
        
        internal Datagram ConvertMessageToDatagram(MessageType messageType, ChannelDescriptor channelDescriptor, UdpRawMessage message)
        {
            Datagram datagram = ConvertMessageToDatagram(messageType, channelDescriptor, message.Message);
            datagram.Channel = message.Channel;
            datagram.DeliveryType = message.DeliveryType;
            return datagram;
        }
        
        internal Datagram ConvertMessageToDatagram(MessageType messageType, ChannelDescriptor channelDescriptor, RawMessage message)
        {
            Datagram datagram = null;
            if (message.Length == 0)
                datagram = CreateDatagramEmpty(messageType, channelDescriptor);
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

        internal UdpRawMessage ConvertDatagramToUdpRawMessage(Datagram datagram)
        {
            var message = ConvertDatagramToRawMessage(datagram);
            return new UdpRawMessage(message, datagram.DeliveryType, datagram.Channel);
        }
        
        internal RawMessage ConvertDatagramToRawMessage(Datagram datagram)
        {
            if (datagram.Length == 0)
                return this.CreateMessageEmpty();
            
            var message = this.CreateMessage(datagram.Length, datagram.Compressed, datagram.Encrypted);
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
                this.Shutdown();
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
            if (lastBoundTo == null)
                throw new InvalidOperationException("Rebind failed: socket never have been bound");
            Bind(lastBoundTo);
        }

        protected void DestroySocket()
        {
            var socket_ = socket;
            if (socket_ != null)
            {
                socket_.Close();
                this.socket = null;
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
            if (socket != null)
                throw new InvalidOperationException("Socket already bound to this peer");
            
            socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.ExclusiveAddressUse = !configuration.ReuseAddress;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, configuration.ReuseAddress);
            socket.Blocking = false;
            
            socket.ReceiveBufferSize = configuration.ReceiveBufferSize;
            socket.SendBufferSize = configuration.SendBufferSize;

            socket.Bind(endPoint);
            lastBoundTo = endPoint;

            try
            {
                const uint IOC_IN = 0x80000000;
                const uint IOC_VENDOR = 0x18000000;
                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                socket.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
            }
            catch
            {
                Logger.Debug("SIO_UDP_CONNRESET not supported on this platform");
                // ignore; SIO_UDP_CONNRESET not supported on this platform
            }

            try
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DontFragment, 1);
            }
            catch
            {
                Logger.Debug("DONT_FRAGMENT not supported on this platform");
            }

            for (int i = 0; i < configuration.NetworkReceiveThreads; i++)
                StartReceive();
        }

        void StartReceive()
        {
            try
            {
                var socket_ = this.socket;
                if (socket_ == null)
                    return;
                SocketAsyncEventArgs arg = socketArgsPool.Pop();
                arg.SetBuffer(arg.Buffer, 0, arg.Buffer.Length);
                if (!socket_.ReceiveFromAsync(arg))
                    IO_Complete(this, arg);
            }
            catch(ObjectDisposedException)
            {
            }
        }

        void EndReceive(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError != SocketError.Success)
                {
                    switch (e.SocketError)
                    {
                        case SocketError.ConnectionReset:
                            // connection reset by peer, aka connection forcibly closed aka "ICMP port unreachable"
                            // we should shut down the connection; but sender seemingly cannot be trusted, so which connection should we shut down?!
                            // So, what to do?
                            return;

                        case SocketError.NotConnected:
                            Logger.Debug($"Socket has been unbound. Rebinding");
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
                }

                if (configuration.ConnectionSimulation != null &&
                    random.NextDouble() < configuration.ConnectionSimulation.PacketLoss)
                {
                    Logger.Debug($"We got a datagram from {e.RemoteEndPoint}, but according to connection simulation rules we dropped it");
                    return;
                }

                ArraySegment<byte> segment = new ArraySegment<byte>(e.Buffer, 0, e.BytesTransferred);
                //Logger.Trace($"Got datagram {segment.Count} bytes from {e.RemoteEndPoint}");
                Datagram datagram = Datagram.Parse(configuration.MemoryManager, segment);
                var ep = new UdpNetEndpoint(e.RemoteEndPoint);

                if (configuration.ConnectionSimulation != null)
                    OnDatagramInternal(datagram, ep);
                else
                    _ = OnDatagramInternalWithSimulationAsync(datagram, ep);
            }
            catch(Exception ex)
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
            if (configuration.ConnectionSimulation != null)
            {
                int delay = configuration.ConnectionSimulation.GetHalfDelay();
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

        internal async Task SendDatagramAsync(UdpConnection connection, Datagram datagram)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (datagram == null)
                throw new ArgumentNullException(nameof(datagram));

            if (configuration.ConnectionSimulation != null)
            {
                if (random.NextDouble() < configuration.ConnectionSimulation.PacketLoss)
                {
                    Logger.Trace($"We're sending {datagram} for {connection}, but according to connection simulation rules we dropped it");
                    return;
                }

                int delay = configuration.ConnectionSimulation.GetHalfDelay();
                if (delay > 0)
                    await Task.Delay(delay).ConfigureAwait(false);
            }

            var socket_ = this.socket;
            if (socket_ == null)
                throw new ObjectDisposedException(nameof(socket), "Socket was disposed");

            SendDatagramAsyncResult operationInfo = new SendDatagramAsyncResult(connection, datagram);
            SocketAsyncEventArgs arg = socketArgsPool.Pop();
            int bytes = datagram.BuildTo(new ArraySegment<byte>(arg.Buffer, 0, arg.Buffer.Length));
            arg.SetBuffer(arg.Buffer, 0, bytes);
            arg.RemoteEndPoint = connection.EndPoint.EndPoint;
            arg.UserToken = operationInfo;
            if (!socket_.SendToAsync(arg))
                IO_Complete(this, arg);
            var result = await operationInfo.Task.ConfigureAwait(false);
            if (result != SocketError.Success)
                throw new SocketException((int) result);
        }

        void EndSend(SocketAsyncEventArgs e)
        {
            try
            {
                SendDatagramAsyncResult opInfo = (SendDatagramAsyncResult)e.UserToken;
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
