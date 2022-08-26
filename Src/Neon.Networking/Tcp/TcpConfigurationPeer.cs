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
        public ConnectionSimulation ConnectionSimulation { get => connectionSimulation; set { CheckLocked(); connectionSimulation = value; } }
        public LingerOption LingerOption { get => lingerOption; set { CheckLocked(); CheckNull(value); lingerOption = value; } }
        public bool NoDelay { get => noDelay; set { CheckLocked(); noDelay = value; } }
        public bool ReuseAddress { get => reuseAddress; set { CheckLocked(); reuseAddress = value; } }
        public int SendBufferSize { get => sendBufferSize; set { CheckLocked(); sendBufferSize = value; } }
        public int ReceiveBufferSize { get => receiveBufferSize; set { CheckLocked(); receiveBufferSize = value; } }
        public IMemoryManager MemoryManager { get => memoryManager; set { CheckLocked(); CheckNull(value); memoryManager = value; } }
        public ILogManager LogManager { get => logManager; set { CheckLocked(); CheckNull(value); logManager = value; } }

        public bool KeepAliveEnabled { get => keepAliveEnabled; set { CheckLocked(); keepAliveEnabled = value; } }
        public int KeepAliveInterval { get => keepAliveInterval; set { CheckLocked(); keepAliveInterval = value; } }
        public int KeepAliveTimeout { get => keepAliveTimeout; set { CheckLocked(); keepAliveTimeout = value; } }

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
            synchronizationContext = new DummySynchronizationContext();
            contextSynchronizationMode = ContextSynchronizationMode.Send;

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
