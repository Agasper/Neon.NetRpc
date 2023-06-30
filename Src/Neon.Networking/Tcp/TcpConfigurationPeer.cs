using System;
using System.Net.Sockets;
using System.Threading;
using Neon.Logging;
using Neon.Util;
using Neon.Util.Pooling;

namespace Neon.Networking.Tcp
{
    public abstract class TcpConfigurationPeer
    {
        /// <summary>
        ///     Allows you to simulate bad network behaviour. Packet loss applied only to UDP (default: null)
        /// </summary>
        public ConnectionSimulation ConnectionSimulation
        {
            get => _connectionSimulation;
            set
            {
                CheckLocked();
                _connectionSimulation = value;
            }
        }

        /// <summary>
        ///     Sets the socket linger options (default: (true, 15))
        /// </summary>
        public LingerOption LingerOption
        {
            get => _lingerOption;
            set
            {
                CheckLocked();
                CheckNull(value);
                _lingerOption = value;
            }
        }

        /// <summary>
        ///     Sets a value that specifies whether the socket is using the Nagle algorithm (default: true)
        /// </summary>
        public bool NoDelay
        {
            get => _noDelay;
            set
            {
                CheckLocked();
                _noDelay = value;
            }
        }

        /// <summary>
        ///     Sets SocketOptionName.ReuseAddress (default: true)
        /// </summary>
        public bool ReuseAddress
        {
            get => _reuseAddress;
            set
            {
                CheckLocked();
                _reuseAddress = value;
            }
        }

        /// <summary>
        ///     Sets socket send buffer size (default: 16384)
        /// </summary>
        public int SendBufferSize
        {
            get => _sendBufferSize;
            set
            {
                CheckLocked();
                _sendBufferSize = value;
            }
        }

        /// <summary>
        ///     Sets socket receive buffer size (default: 16384)
        /// </summary>
        public int ReceiveBufferSize
        {
            get => _receiveBufferSize;
            set
            {
                CheckLocked();
                _receiveBufferSize = value;
            }
        }

        /// <summary>
        ///     A manager which provide us streams and arrays for a temporary use (default: MemoryManager.Shared)
        /// </summary>
        public IMemoryManager MemoryManager
        {
            get => _memoryManager;
            set
            {
                CheckLocked();
                CheckNull(value);
                _memoryManager = value;
            }
        }

        /// <summary>
        ///     Log manager for network logs (default: LogManager.Dummy)
        /// </summary>
        public ILogManager LogManager
        {
            get => _logManager;
            set
            {
                CheckLocked();
                CheckNull(value);
                _logManager = value;
            }
        }

        /// <summary>
        ///     If true we will send ping packets to the other peer every KeepAliveInterval (default: true)
        /// </summary>
        public bool KeepAliveEnabled
        {
            get => _keepAliveEnabled;
            set
            {
                CheckLocked();
                _keepAliveEnabled = value;
            }
        }

        /// <summary>
        ///     Interval for pings (default: 1000)
        /// </summary>
        public int KeepAliveInterval
        {
            get => _keepAliveInterval;
            set
            {
                CheckLocked();
                _keepAliveInterval = value;
            }
        }

        /// <summary>
        ///     If we haven't got response for ping within timeout, we drop the connection (default: 10000)
        /// </summary>
        public int KeepAliveTimeout
        {
            get => _keepAliveTimeout;
            set
            {
                CheckLocked();
                _keepAliveTimeout = value;
            }
        }

        /// <summary>
        ///     Context synchronizations mode Send ot Post (default: Post)
        /// </summary>
        public ContextSynchronizationMode ContextSynchronizationMode
        {
            get => _contextSynchronizationMode;
            set
            {
                CheckLocked();
                _contextSynchronizationMode = value;
            }
        }

        protected ConnectionSimulation _connectionSimulation;
        protected ContextSynchronizationMode _contextSynchronizationMode;
        protected bool _keepAliveEnabled;
        protected int _keepAliveInterval;
        protected int _keepAliveTimeout;
        protected LingerOption _lingerOption;

        protected bool _locked;
        protected ILogManager _logManager;
        protected IMemoryManager _memoryManager;
        protected bool _noDelay;
        protected int _receiveBufferSize;
        protected bool _reuseAddress;

        //internal SynchronizationContext SyncronizationContext => syncronizationContext;

        protected int _sendBufferSize;
        protected SynchronizationContext _synchronizationContext;

        public TcpConfigurationPeer()
        {
            _synchronizationContext = new NeonSynchronizationContext();
            _memoryManager = Util.Pooling.MemoryManager.Shared;
            _sendBufferSize = 16384;
            _receiveBufferSize = 16384;
            _reuseAddress = true;
            _noDelay = true;
            _logManager = Logging.LogManager.Default;
            _lingerOption = new LingerOption(true, 15);
            _keepAliveEnabled = true;
            _keepAliveInterval = 1000;
            _keepAliveTimeout = 10000;
        }

        internal void Lock()
        {
            Validate();
            if (_locked)
                throw new InvalidOperationException($"{nameof(TcpConfigurationPeer)} already locked");
            _locked = true;
        }

        protected void CheckLocked()
        {
            if (_locked)
                throw new InvalidOperationException("Configuration is locked in read only mode");
        }

        protected void CheckNull(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
        }

        public virtual void CaptureSynchronizationContext()
        {
            CheckLocked();
            if (SynchronizationContext.Current == null)
                throw new NullReferenceException("Synchronization context is null");
            _synchronizationContext = SynchronizationContext.Current;
        }

        public virtual void SetSynchronizationContext(SynchronizationContext context)
        {
            CheckLocked();
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            _synchronizationContext = context;
        }

        internal void Synchronize(SendOrPostCallback callback, object state)
        {
            ContextSynchronizationHelper.Synchronize(_synchronizationContext, _contextSynchronizationMode,
                callback, state);
        }

        internal void Synchronize(ContextSynchronizationMode mode, SendOrPostCallback callback, object state)
        {
            ContextSynchronizationHelper.Synchronize(_synchronizationContext, mode,
                callback, state);
        }

        internal void SynchronizeSafe(ILogger logger, string nameForLogs, SendOrPostCallback callback, object state)
        {
            ContextSynchronizationHelper.SynchronizeSafe(_synchronizationContext, _contextSynchronizationMode,
                logger, nameForLogs, callback, state);
        }

        internal virtual void Validate()
        {
            if (KeepAliveEnabled && KeepAliveTimeout <= KeepAliveInterval)
                throw new ArgumentException(
                    $"{nameof(KeepAliveTimeout)} must be greater than {nameof(KeepAliveInterval)}");
            if (ReceiveBufferSize < 1024)
                throw new ArgumentException(
                    $"{nameof(ReceiveBufferSize)} must be equal or greater than 1024 bytes");
            if (SendBufferSize < 1024)
                throw new ArgumentException(
                    $"{nameof(SendBufferSize)} must be equal or greater than 1024 bytes");
        }
    }
}