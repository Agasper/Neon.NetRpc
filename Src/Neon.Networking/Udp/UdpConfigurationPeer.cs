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
        /// Log manager for network logs (default: LogManager.Dummy)
        /// </summary>
        public ILogManager LogManager { get => logManager; set { CheckLocked(); CheckNull(value); logManager = value; } }
        /// <summary>
        /// A manager who provide us streams and arrays for a temporary use (default: MemoryManager.Shared)
        /// </summary>
        public IMemoryManager MemoryManager { get => memoryManager; set { CheckLocked(); CheckNull(value); memoryManager = value; } }
        /// <summary>
        /// Allows you to simulate bad network behaviour (default: null)
        /// </summary>
        public ConnectionSimulation ConnectionSimulation { get => connectionSimulation; set { CheckLocked(); connectionSimulation = value; } }
        /// <summary>
        /// Set socket send buffer size (default: 65535)
        /// </summary>
        public int SendBufferSize { get => sendBufferSize; set { CheckLocked(); sendBufferSize = value; } }
        /// <summary>
        /// Set socket receive buffer size (default: 1048676)
        /// </summary>
        public int ReceiveBufferSize { get => receiveBufferSize; set { CheckLocked(); receiveBufferSize = value; } }
        /// <summary>
        /// Set SocketOptionName.ReuseAddress (default: true)
        /// </summary>
        public bool ReuseAddress { get => reuseAddress; set { CheckLocked(); reuseAddress = value; } }
        /// <summary>
        /// If no packets received within this timeout, connection considered dead (default: 10000)
        /// </summary>
        public int ConnectionTimeout { get => connectionTimeout; set { CheckLocked(); connectionTimeout = value; } }
        /// <summary>
        /// The amount of threads for grabbing packets from the socket (default: Math.Max(1, Environment.ProcessorCount - 1))
        /// </summary>
        public int NetworkReceiveThreads { get => networkReceiveThreads; set { CheckLocked(); networkReceiveThreads = value; } }
        /// <summary>
        /// Do not expand MTU greater than this value (default: int.MaxValue)
        /// </summary>
        public int LimitMtu { get => limitMtu; set { CheckLocked(); limitMtu = value; } }
        /// <summary>
        /// Interval for pings (default: 1000)
        /// </summary>
        public int KeepAliveInterval { get => keepAliveInterval; set { CheckLocked(); keepAliveInterval = value; } }
        /// <summary>
        /// Connection will be destroyed after linger timeout (default: 60000)
        /// </summary>
        public int ConnectionLingerTimeout { get => connectionLingerTimeout; set { CheckLocked(); connectionLingerTimeout = value; } }
        /// <summary>
        /// If you try to send too large unreliable message (that couldn't be fragmented) what we should do: drop it ot throw an exception (default: RaiseException)
        /// </summary>
        public TooLargeMessageBehaviour TooLargeUnreliableMessageBehaviour { get => tooLargeMessageBehaviour; set { CheckLocked(); tooLargeMessageBehaviour = value; } }
        /// <summary>
        /// Context synchronizations mode Send ot Post (default: Post)
        /// </summary>
        public ContextSynchronizationMode ContextSynchronizationMode { get => contextSynchronizationMode; set { CheckLocked(); contextSynchronizationMode = value; } }

        //internal SynchronizationContext SyncronizationContext => syncronizationContext;

        int sendBufferSize;
        int receiveBufferSize;
        bool reuseAddress;
        IMemoryManager memoryManager;
        ILogManager logManager;
        int connectionTimeout;
        int connectionLingerTimeout;
        int networkReceiveThreads;
        int limitMtu;
        int keepAliveInterval;
        ConnectionSimulation connectionSimulation;
        TooLargeMessageBehaviour tooLargeMessageBehaviour;

        SynchronizationContext synchronizationContext;
        ContextSynchronizationMode contextSynchronizationMode;

        private protected bool locked;

        internal void Lock()
        {
            Validate();
            if (locked)
                throw new InvalidOperationException($"{nameof(UdpConfigurationPeer)} already locked");
            locked = true;
        }
        
        internal virtual void Validate()
        {
            if (this.KeepAliveInterval < 1)
                throw new ArgumentException(
                    $"{nameof(this.KeepAliveInterval)} must be greater than 0");
            if (this.ConnectionLingerTimeout < 0)
                throw new ArgumentException(
                    $"{nameof(this.ConnectionLingerTimeout)} must be equal or greater than 0");
            if (this.NetworkReceiveThreads < 1)
                throw new ArgumentException(
                    $"{nameof(this.NetworkReceiveThreads)} must be greater than 0");
            if (this.ConnectionTimeout <= this.KeepAliveInterval)
                throw new ArgumentException(
                    $"{nameof(this.ConnectionTimeout)} must be greater than {nameof(this.KeepAliveInterval)}");
            if (this.ReceiveBufferSize < 1024)
                throw new ArgumentException(
                    $"{nameof(this.ReceiveBufferSize)} must be equal or greater than 1024 bytes");
            if (this.SendBufferSize < 1024)
                throw new ArgumentException(
                    $"{nameof(this.SendBufferSize)} must be equal or greater than 1024 bytes");
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

        internal void SynchronizeSafe(ILogger logger, string nameForLogs, SendOrPostCallback callback, object state)
        {
            ContextSynchronizationHelper.SynchronizeSafe(this.synchronizationContext, this.contextSynchronizationMode,
                logger, nameForLogs, callback, state);
        }

        public UdpConfigurationPeer()
        {
            sendBufferSize = 65535;
            receiveBufferSize = 1048676;
            reuseAddress = true;
            synchronizationContext = new NeonSynchronizationContext();
            tooLargeMessageBehaviour = TooLargeMessageBehaviour.RaiseException;
            contextSynchronizationMode = ContextSynchronizationMode.Post;
            networkReceiveThreads = Math.Max(1, Environment.ProcessorCount - 1);
            memoryManager = Neon.Util.Pooling.MemoryManager.Shared;
            logManager = Logging.LogManager.Dummy;
            connectionTimeout = 5000;
            keepAliveInterval = 1000;
            connectionLingerTimeout = 60000;
            limitMtu = int.MaxValue;
        }
    }
}
