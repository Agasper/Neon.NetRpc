using System;
using System.Threading;
using Neon.Logging;
using Neon.Util;
using Neon.Util.Pooling;

namespace Neon.Networking.Udp
{
    public class UdpConfigurationPeer
    {
        public enum TooLargeMessageBehaviour
        {
            Drop,
            RaiseException
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
        ///     A manager who provide us streams and arrays for a temporary use (default: MemoryManager.Shared)
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
        ///     Allows you to simulate bad network behaviour (default: null)
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
        ///     Set socket send buffer size (default: 65535)
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
        ///     Set socket receive buffer size (default: 1048676)
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
        ///     Set SocketOptionName.ReuseAddress (default: true)
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
        ///     If no packets received within this timeout, connection considered dead (default: 10000)
        /// </summary>
        public int ConnectionTimeout
        {
            get => _connectionTimeout;
            set
            {
                CheckLocked();
                _connectionTimeout = value;
            }
        }

        /// <summary>
        ///     The amount of threads for grabbing packets from the socket (default: Math.Max(1, Environment.ProcessorCount - 1))
        /// </summary>
        public int NetworkReceiveThreads
        {
            get => _networkReceiveThreads;
            set
            {
                CheckLocked();
                _networkReceiveThreads = value;
            }
        }

        /// <summary>
        ///     Do not expand MTU greater than this value (default: int.MaxValue)
        /// </summary>
        public int LimitMtu
        {
            get => _limitMtu;
            set
            {
                CheckLocked();
                _limitMtu = value;
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
        ///     Connection will be destroyed after linger timeout (default: 60000)
        /// </summary>
        public int ConnectionLingerTimeout
        {
            get => _connectionLingerTimeout;
            set
            {
                CheckLocked();
                _connectionLingerTimeout = value;
            }
        }

        /// <summary>
        ///     If you try to send too large unreliable message (that couldn't be fragmented) what we should do: drop it ot throw
        ///     an exception (default: RaiseException)
        /// </summary>
        public TooLargeMessageBehaviour TooLargeUnreliableMessageBehaviour
        {
            get => _tooLargeMessageBehaviour;
            set
            {
                CheckLocked();
                _tooLargeMessageBehaviour = value;
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

        int _connectionLingerTimeout;
        ConnectionSimulation _connectionSimulation;
        int _connectionTimeout;
        ContextSynchronizationMode _contextSynchronizationMode;
        int _keepAliveInterval;
        int _limitMtu;

        private protected bool _locked;
        ILogManager _logManager;
        IMemoryManager _memoryManager;
        int _networkReceiveThreads;
        int _receiveBufferSize;
        bool _reuseAddress;

        //internal SynchronizationContext SyncronizationContext => syncronizationContext;

        int _sendBufferSize;

        SynchronizationContext _synchronizationContext;
        TooLargeMessageBehaviour _tooLargeMessageBehaviour;

        public UdpConfigurationPeer()
        {
            _sendBufferSize = 65535;
            _receiveBufferSize = 1048676;
            _reuseAddress = true;
            _synchronizationContext = new NeonSynchronizationContext();
            _tooLargeMessageBehaviour = TooLargeMessageBehaviour.RaiseException;
            _networkReceiveThreads = Math.Max(1, Environment.ProcessorCount - 1);
            _memoryManager = Util.Pooling.MemoryManager.Shared;
            _logManager = Logging.LogManager.Default;
            _connectionTimeout = 5000;
            _keepAliveInterval = 1000;
            _connectionLingerTimeout = 60000;
            _limitMtu = int.MaxValue;
        }

        internal void Lock()
        {
            Validate();
            if (_locked)
                throw new InvalidOperationException($"{nameof(UdpConfigurationPeer)} already locked");
            _locked = true;
        }

        internal virtual void Validate()
        {
            if (KeepAliveInterval < 1)
                throw new ArgumentException(
                    $"{nameof(KeepAliveInterval)} must be greater than 0");
            if (ConnectionLingerTimeout < 0)
                throw new ArgumentException(
                    $"{nameof(ConnectionLingerTimeout)} must be equal or greater than 0");
            if (NetworkReceiveThreads < 1)
                throw new ArgumentException(
                    $"{nameof(NetworkReceiveThreads)} must be greater than 0");
            if (ConnectionTimeout <= KeepAliveInterval)
                throw new ArgumentException(
                    $"{nameof(ConnectionTimeout)} must be greater than {nameof(KeepAliveInterval)}");
            if (ReceiveBufferSize < 1024)
                throw new ArgumentException(
                    $"{nameof(ReceiveBufferSize)} must be equal or greater than 1024 bytes");
            if (SendBufferSize < 1024)
                throw new ArgumentException(
                    $"{nameof(SendBufferSize)} must be equal or greater than 1024 bytes");
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

        internal void SynchronizeSafe(ILogger logger, string nameForLogs, SendOrPostCallback callback, object state)
        {
            ContextSynchronizationHelper.SynchronizeSafe(_synchronizationContext, _contextSynchronizationMode,
                logger, nameForLogs, callback, state);
        }
    }
}