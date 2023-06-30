using System;
using System.Net.Sockets;
using Neon.Logging;
using Neon.Networking.Messages;
using Neon.Networking.Tcp.Events;
using Neon.Util.Polling;

namespace Neon.Networking.Tcp
{
    public abstract class TcpPeer
    {
        /// <summary>
        ///     A user defined tag
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        ///     Current peer configuration
        /// </summary>
        public TcpConfigurationPeer Configuration => _configuration;

        /// <summary>
        ///     Returns true is peer has been started, false if not
        /// </summary>
        public bool IsStarted => _poller.IsStarted;

        private protected abstract ILogger Logger { get; }
        private protected readonly TcpConfigurationPeer _configuration;
        readonly Poller _poller;

        public TcpPeer(TcpConfigurationPeer configuration)
        {
            configuration.Lock();
            _configuration = configuration;
            _poller = new Poller(5, PollEventsInternal_);
        }

        /// <summary>
        ///     Start an internal network thread
        /// </summary>
        public virtual void Start()
        {
            _poller.StartPolling();
        }

        /// <summary>
        ///     Stop an internal network thread
        /// </summary>
        public virtual void Shutdown()
        {
            _poller.StopPolling(false);
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
            socket.ReceiveBufferSize = _configuration.ReceiveBufferSize;
            socket.SendBufferSize = _configuration.SendBufferSize;
            socket.NoDelay = _configuration.NoDelay;
            socket.Blocking = false;
            if (_configuration.LingerOption != null)
                socket.LingerState = _configuration.LingerOption;
        }

        protected Socket GetNewSocket(AddressFamily addressFamily)
        {
            var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.ExclusiveAddressUse = !_configuration.ReuseAddress;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress,
                _configuration.ReuseAddress);
            socket.Blocking = false;
            return socket;
        }

        /// <summary>
        ///     Creating a new empty message with a length defaulted by memory manager
        /// </summary>
        /// <returns>A new message</returns>
        public RawMessage CreateMessage()
        {
            return new RawMessage(_configuration.MemoryManager);
        }

        /// <summary>
        ///     Creating a new empty message based on existing data. Data will be copied to the new message
        /// </summary>
        /// <returns>A new message</returns>
        public RawMessage CreateMessage(ArraySegment<byte> segment)
        {
            return new RawMessage(_configuration.MemoryManager, segment);
        }

        /// <summary>
        ///     Creating a new empty message with a predefined size. Returning message size may be bigger than requested value
        /// </summary>
        /// <param name="length">Size in bytes</param>
        /// <returns>A new message</returns>
        public RawMessage CreateMessage(int length)
        {
            return new RawMessage(_configuration.MemoryManager, length);
        }

        internal virtual void OnConnectionClosedInternal(TcpConnection tcpConnection, Exception ex)
        {
            var args = new ConnectionClosedEventArgs(tcpConnection, ex);

            _configuration.SynchronizeSafe(Logger,
                $"{nameof(TcpConnection)}.{nameof(args.Connection.OnConnectionClosed)}",
                state =>
                {
                    var args_ = state as ConnectionClosedEventArgs;
                    args_.Connection.OnConnectionClosed(args_);
                }, args
            );

            _configuration.SynchronizeSafe(Logger, $"{nameof(TcpPeer)}.{nameof(OnConnectionClosed)}",
                state => OnConnectionClosed(state as ConnectionClosedEventArgs), args
            );
        }

        internal void OnConnectionOpenedInternal(TcpConnection tcpConnection)
        {
            var args = new ConnectionOpenedEventArgs(tcpConnection);

            _configuration.SynchronizeSafe(Logger,
                $"{nameof(TcpConnection)}.{nameof(args.Connection.OnConnectionOpened)}",
                state =>
                {
                    var args_ = state as ConnectionOpenedEventArgs;
                    args_.Connection.OnConnectionOpened(args_);
                }, args
            );

            _configuration.SynchronizeSafe(Logger, $"{nameof(TcpPeer)}.{nameof(OnConnectionOpened)}",
                state => OnConnectionOpened(state as ConnectionOpenedEventArgs), args
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
            catch (Exception ex)
            {
                Logger.Critical("Exception in polling thread: " + ex);
                Shutdown();
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