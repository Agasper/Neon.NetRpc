using System;
using System.Net.Sockets;
using Neon.Networking.Tcp.Events;
using Neon.Logging;
using Neon.Networking.Messages;
using Neon.Util.Polling;

namespace Neon.Networking.Tcp
{
    public abstract class TcpPeer
    {
        /// <summary>
        /// A user defined tag
        /// </summary>
        public object Tag { get; set; }
        /// <summary>
        /// Current peer configuration
        /// </summary>
        public TcpConfigurationPeer Configuration => configuration;
        /// <summary>
        /// Returns true is peer has been started, false if not
        /// </summary>
        public bool IsStarted => poller.IsStarted;

        private protected readonly TcpConfigurationPeer configuration;
        private protected abstract ILogger Logger { get; }
        readonly Poller poller;

        public TcpPeer(TcpConfigurationPeer configuration)
        {
            configuration.Lock();
            this.configuration = configuration;
            this.poller = new Poller(5, PollEventsInternal_);
        }

        /// <summary>
        /// Start an internal network thread
        /// </summary>
        public virtual void Start()
        {
            this.poller.StartPolling();
        }

        /// <summary>
        /// Stop an internal network thread
        /// </summary>
        public virtual void Shutdown()
        {
            this.poller.StopPolling(false);
        }

        protected void CheckStarted()
        {
            if (!IsStarted)
                throw new InvalidOperationException("Please call Start() first");
        }

        protected virtual TcpConnection CreateConnection()
        {
            return new TcpConnection(this);
        }

        protected void SetSocketOptions(Socket socket)
        {
            socket.ReceiveBufferSize = configuration.ReceiveBufferSize;
            socket.SendBufferSize = configuration.SendBufferSize;
            socket.NoDelay = configuration.NoDelay;
            socket.Blocking = false;
            if (configuration.LingerOption != null)
                socket.LingerState = configuration.LingerOption;
        }

        protected Socket GetNewSocket(AddressFamily addressFamily)
        {
            Socket socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.ExclusiveAddressUse = !configuration.ReuseAddress;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, configuration.ReuseAddress);
            socket.Blocking = false;
            return socket;
        }

        /// <summary>
        /// Creating a new empty message with a length defaulted my memory manager 
        /// </summary>
        /// <returns>A new message</returns>
        public RawMessage CreateMessage()
        {
            Guid newGuid = Guid.NewGuid();
            return new RawMessage(configuration.MemoryManager, configuration.MemoryManager.GetStream(newGuid), false, false, newGuid);
        }

        /// <summary>
        /// Creating a new empty message based on existing data. Data will be copied to the new message
        /// </summary>
        /// <returns>A new message</returns>
        public RawMessage CreateMessage(ArraySegment<byte> segment)
        {
            Guid newGuid = Guid.NewGuid();
            return new RawMessage(configuration.MemoryManager, configuration.MemoryManager.GetStream(segment, newGuid), false, false, newGuid);
        }

        /// <summary>
        /// Creating a new empty message with a predefined size. Returning message size may be bigger than requested value
        /// </summary>
        /// <param name="size">Size in bytes</param>
        /// <returns>A new message</returns>
        public RawMessage CreateMessage(int size)
        {
            Guid newGuid = Guid.NewGuid();
            return new RawMessage(configuration.MemoryManager, configuration.MemoryManager.GetStream(size, newGuid), false, false, newGuid);
        }

        internal virtual void OnConnectionClosedInternal(TcpConnection tcpConnection, Exception ex)
        {
            var args = new ConnectionClosedEventArgs(tcpConnection, ex);
            
            configuration.SynchronizeSafe(Logger, $"{nameof(TcpConnection)}.{nameof(args.Connection.OnConnectionClosed)}",
                (state) =>
                {
                    var args_ = state as ConnectionClosedEventArgs;
                    args_.Connection.OnConnectionClosed(args_);
                }, args
            );
            
            configuration.SynchronizeSafe(Logger, $"{nameof(TcpPeer)}.{nameof(OnConnectionClosed)}",
                (state) => OnConnectionClosed(state as ConnectionClosedEventArgs), args
            );
        }

        internal void OnConnectionOpenedInternal(TcpConnection tcpConnection)
        {
            ConnectionOpenedEventArgs args = new ConnectionOpenedEventArgs(tcpConnection);
            
            configuration.SynchronizeSafe(Logger, $"{nameof(TcpConnection)}.{nameof(args.Connection.OnConnectionOpened)}",
                (state) =>
                {
                    var args_ = state as ConnectionOpenedEventArgs;
                    args_.Connection.OnConnectionOpened(args_);
                }, args
            );
            
            configuration.SynchronizeSafe(Logger, $"{nameof(TcpPeer)}.{nameof(OnConnectionOpened)}",
                (state) => OnConnectionOpened(state as ConnectionOpenedEventArgs), args
            );
        }
        
        protected virtual void OnConnectionClosed(ConnectionClosedEventArgs args)
        {

        }

        protected virtual void OnConnectionOpened(ConnectionOpenedEventArgs args)
        {

        }

        void PollEventsInternal_()
        {
            try
            {
                PollEventsInternal();
                PollEvents();
            }
            catch(Exception ex)
            {
                Logger.Critical("Exception in polling thread: " + ex.ToString());
                this.Shutdown();
            }
        }

        private protected virtual void PollEventsInternal()
        {
            
        }

        protected virtual void PollEvents()
        {
            
        }

    }
}
