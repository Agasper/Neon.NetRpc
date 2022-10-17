using System;
using System.Buffers;
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
        /// Allows you to simulate bad network behaviour. Packet loss applied only to UDP (default: null)
        /// </summary>
        public ConnectionSimulation ConnectionSimulation { get => connectionSimulation; set { CheckLocked(); connectionSimulation = value; } }
        /// <summary>
        /// Sets the socket linger options (default: (true, 15))
        /// </summary>
        public LingerOption LingerOption { get => lingerOption; set { CheckLocked(); CheckNull(value); lingerOption = value; } }
        /// <summary>
        /// Sets a value that specifies whether the socket is using the Nagle algorithm (default: true)
        /// </summary>
        public bool NoDelay { get => noDelay; set { CheckLocked(); noDelay = value; } }
        /// <summary>
        /// Sets SocketOptionName.ReuseAddress (default: true)
        /// </summary>
        public bool ReuseAddress { get => reuseAddress; set { CheckLocked(); reuseAddress = value; } }
        /// <summary>
        /// Sets socket send buffer size (default: 16384)
        /// </summary>
        public int SendBufferSize { get => sendBufferSize; set { CheckLocked(); sendBufferSize = value; } }
        /// <summary>
        /// Sets socket receive buffer size (default: 16384)
        /// </summary>
        public int ReceiveBufferSize { get => receiveBufferSize; set { CheckLocked(); receiveBufferSize = value; } }
        /// <summary>
        /// A manager which provide us streams and arrays for a temporary use (default: MemoryManager.Shared)
        /// </summary>
        public IMemoryManager MemoryManager { get => memoryManager; set { CheckLocked(); CheckNull(value); memoryManager = value; } }
        /// <summary>
        /// Log manager for network logs (default: LogManager.Dummy)
        /// </summary>
        public ILogManager LogManager { get => logManager; set { CheckLocked(); CheckNull(value); logManager = value; } }

        /// <summary>
        /// If true we will send ping packets to the other peer every KeepAliveInterval (default: true)
        /// </summary>
        public bool KeepAliveEnabled { get => keepAliveEnabled; set { CheckLocked(); keepAliveEnabled = value; } }
        /// <summary>
        /// Interval for pings (default: 1000)
        /// </summary>
        public int KeepAliveInterval { get => keepAliveInterval; set { CheckLocked(); keepAliveInterval = value; } }
        /// <summary>
        /// If we haven't got response for ping within timeout, we drop the connection (default: 10000)
        /// </summary>
        public int KeepAliveTimeout { get => keepAliveTimeout; set { CheckLocked(); keepAliveTimeout = value; } }

        /// <summary>
        /// Context synchronizations mode Send ot Post (default: Post)
        /// </summary>
        public ContextSynchronizationMode ContextSynchronizationMode { get => contextSynchronizationMode; set { CheckLocked(); contextSynchronizationMode = value; } }

        //internal SynchronizationContext SyncronizationContext => syncronizationContext;

        protected int sendBufferSize;
        protected int receiveBufferSize;
        protected bool noDelay;
        protected IMemoryManager memoryManager;
        protected LingerOption lingerOption;
        protected ILogManager logManager;
        protected bool keepAliveEnabled;
        protected int keepAliveInterval;
        protected int keepAliveTimeout;
        protected bool reuseAddress;

        protected ConnectionSimulation connectionSimulation;
        protected SynchronizationContext synchronizationContext;
        protected ContextSynchronizationMode contextSynchronizationMode;

        protected bool locked;

        internal void Lock()
        {
            Validate();
            if (locked)
                throw new InvalidOperationException($"{nameof(TcpConfigurationPeer)} already locked");
            locked = true;
        }

        protected void CheckLocked()
        {
            if (locked)
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
            this.synchronizationContext = SynchronizationContext.Current;
        }

        public virtual void SetSynchronizationContext(SynchronizationContext context)
        {
            CheckLocked();
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            this.synchronizationContext = context;
        }
        
        internal void Synchronize(SendOrPostCallback callback, object state)
        {
            ContextSynchronizationHelper.Synchronize(this.synchronizationContext, this.contextSynchronizationMode,
                callback, state);
        }
        
        internal void Synchronize(ContextSynchronizationMode mode, SendOrPostCallback callback, object state)
        {
            ContextSynchronizationHelper.Synchronize(this.synchronizationContext, mode,
                callback, state);
        }

        internal void SynchronizeSafe(ILogger logger, string nameForLogs, SendOrPostCallback callback, object state)
        {
            ContextSynchronizationHelper.SynchronizeSafe(this.synchronizationContext, this.contextSynchronizationMode,
                logger, nameForLogs, callback, state);
        }

        internal virtual void Validate()
        {
            if (this.KeepAliveEnabled && this.KeepAliveTimeout <= this.KeepAliveInterval)
                throw new ArgumentException(
                    $"{nameof(this.KeepAliveTimeout)} must be greater than {nameof(this.KeepAliveInterval)}");
            if (this.ReceiveBufferSize < 1024)
                throw new ArgumentException(
                    $"{nameof(this.ReceiveBufferSize)} must be equal or greater than 1024 bytes");
            if (this.SendBufferSize < 1024)
                throw new ArgumentException(
                    $"{nameof(this.SendBufferSize)} must be equal or greater than 1024 bytes");
        }

        public TcpConfigurationPeer()
        {
            synchronizationContext = new NeonSynchronizationContext();
            contextSynchronizationMode = ContextSynchronizationMode.Post;

            memoryManager = Neon.Util.Pooling.MemoryManager.Shared;
            sendBufferSize = 16384;
            receiveBufferSize = 16384;
            reuseAddress = true;
            noDelay = true;
            logManager = Logging.LogManager.Dummy;
            lingerOption = new LingerOption(true, 15);
            keepAliveEnabled = true;
            keepAliveInterval = 1000;
            keepAliveTimeout = 10000;
        }
    }
}
