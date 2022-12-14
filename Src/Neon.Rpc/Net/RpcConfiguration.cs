using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Neon.Rpc.Cryptography;
using Neon.Rpc.Serialization;
using Neon.Logging;
using Neon.Networking;
using Neon.Networking.Cryptography;
using Neon.Util;
using Neon.Util.Pooling;

namespace Neon.Rpc.Net
{
    public class RpcConfiguration
    {
        internal TaskScheduler TaskScheduler => taskScheduler;
     
        /// <summary>
        /// Default execution timeout for all methods (default: -1/infinite)
        /// </summary>
        public int DefaultExecutionTimeout { get => defaultExecutionTimeout; set { CheckLocked(); CheckNull(value); defaultExecutionTimeout = value; } }
        /// <summary>
        /// Should methods be executed in the same order as come, or we can execute they simultaneously (default: false)
        /// </summary>
        public bool OrderedExecution { get => orderedExecution; set { CheckLocked(); CheckNull(value); orderedExecution = value; } }
        /// <summary>
        /// If ordered execution is set, sets the maximum execution queue. Trying to execute method above limit cause exception (default: 32)
        /// </summary>
        public int OrderedExecutionMaxQueue { get => orderedExecutionMaxQueue; set { CheckLocked(); CheckNull(value); orderedExecutionMaxQueue = value; } }
        /// <summary>
        /// Serializer for primitives and Protobuf messages (default: an empty one)
        /// </summary>
        public IRpcSerializer Serializer { get => serializer; set { CheckLocked(); CheckNull(value); serializer = value; } }
        /// <summary>
        /// Log manager for RPC logs (default: LogManager.Dummy)
        /// </summary>
        public ILogManager LogManager { get => logManager; set { CheckLocked(); CheckNull(value); logManager = value; } }
        /// <summary>
        /// User-defined session factory (default: null)
        /// </summary>
        public ISessionFactory SessionFactory { get => sessionFactory; set { CheckLocked(); CheckNull(value); sessionFactory = value; } }
        /// <summary>
        /// Configures the invocation rules (default: AllowLambdaExpressions = true, AllowNonPublicMethods = true)
        /// </summary>
        public RemotingInvocationRules RemotingInvocationRules { get => remotingInvocationRules; set { CheckLocked(); CheckNull(value); remotingInvocationRules = value; } }
        /// <summary>
        /// Threshold in bytes to compress your data (default: 1024)
        /// </summary>
        public int CompressionThreshold { get => compressionThreshold; set { CheckLocked(); compressionThreshold = value; } }
        /// <summary>
        /// Context synchronization mode: Send ro Post (default: Post)
        /// </summary>
        public ContextSynchronizationMode ContextSynchronizationMode { get => contextSynchronizationMode; set { CheckLocked(); contextSynchronizationMode = value; } }

        ICipherFactory cipherFactory;
        ISessionFactory sessionFactory;
        ILogManager logManager;
        int defaultExecutionTimeout;
        bool orderedExecution;
        int orderedExecutionMaxQueue;
        int compressionThreshold;
        IRpcSerializer serializer;
        TaskScheduler taskScheduler;
        SynchronizationContext synchronizationContext;
        RemotingInvocationRules remotingInvocationRules;
        ContextSynchronizationMode contextSynchronizationMode;
        
        protected bool locked;

        internal virtual void Lock()
        {
            if (locked)
                throw new InvalidOperationException($"{nameof(RpcConfiguration)} already locked");
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
            this.taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }
        
        public virtual void SetSynchronizationContext(SynchronizationContext context)
        {
            CheckLocked();
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            this.synchronizationContext = context;
            this.taskScheduler = new SynchronizationContextTaskScheduler(context);
        }
        
        internal void SynchronizeSafe(ILogger logger, string nameForLogs, SendOrPostCallback callback, object state)
        {
            ContextSynchronizationHelper.SynchronizeSafe(this.synchronizationContext, this.contextSynchronizationMode,
                logger, nameForLogs, callback, state);
        }

        internal bool IsCipherSet => this.cipherFactory != null;

        internal ICipher CreateNewCipher()
        {
            if (this.cipherFactory == null)
                throw new NullReferenceException("Cipher not set");
            return this.cipherFactory.CreateNewCipher();
        }

        public void SetCipher<T>() where T : ICipher, new()
        {
            CheckLocked();
            this.cipherFactory = new CipherFactory<T>();
        }

        public RpcConfiguration()
        {
            Assembly assembly = Assembly.GetEntryAssembly();
            serializer = new RpcSerializer(MemoryManager.Shared);
            remotingInvocationRules = RemotingInvocationRules.Default;
            synchronizationContext = new NeonSynchronizationContext();
            contextSynchronizationMode = ContextSynchronizationMode.Post;
            taskScheduler = TaskScheduler.Default;
            logManager = Logging.LogManager.Dummy;
            defaultExecutionTimeout = -1;
            orderedExecution = false;
            orderedExecutionMaxQueue = 32;
            compressionThreshold = 1024;
        }
    }
}