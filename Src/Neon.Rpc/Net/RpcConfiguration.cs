using System;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking;
using Neon.Networking.Cryptography;
using Neon.Rpc.Cryptography;
using Neon.Rpc.Cryptography.Ciphers;
using Neon.Util;
using Neon.Util.Pooling;

namespace Neon.Rpc.Net
{
    public abstract class RpcConfiguration
    {
        internal TaskScheduler TaskScheduler => _taskScheduler;
        public CryptographyConfiguration CryptographyConfiguration { get => _cryptographyConfiguration; set { CheckLocked(); CheckNull(value); _cryptographyConfiguration = value; } }
        
        public virtual IMemoryManager MemoryManager { get => _memoryManager; set { CheckLocked(); CheckNull(value); _memoryManager = value; } }
        
        public bool LogMessageBodyDebug { get => _logMessageBodyDebug; set { CheckLocked(); CheckNull(value); _logMessageBodyDebug = value; } }
        /// <summary>
        /// Should methods be executed in the same order as come, or we can execute they simultaneously (default: false)
        /// </summary>
        public bool OrderedExecution { get => _orderedExecution; set { CheckLocked(); CheckNull(value); _orderedExecution = value; } }
        /// <summary>
        /// If ordered execution is set, sets the maximum execution queue. Trying to execute method above limit cause exception (default: 32)
        /// </summary>
        public int OrderedExecutionMaxQueue { get => _orderedExecutionMaxQueue; set { CheckLocked(); CheckNull(value); _orderedExecutionMaxQueue = value; } }
        /// <summary>
        /// Log manager (default: LogManager.Default)
        /// </summary>
        public virtual ILogManager LogManager { get => _logManager; set { CheckLocked(); CheckNull(value); _logManager = value; } }
        /// <summary>
        /// User-defined session factory (default: null)
        /// </summary>
        public ISessionFactory SessionFactory { get => _sessionFactory; set { CheckLocked(); CheckNull(value); _sessionFactory = value; } }
        /// <summary>
        /// Configures the invocation rules (default: AllowLambdaExpressions = true, AllowNonPublicMethods = true)
        /// </summary>
        public RpcInvocationRules RpcInvocationRules { get => _rpcInvocationRules; set { CheckLocked(); CheckNull(value); _rpcInvocationRules = value; } }
        /// <summary>
        /// Threshold in bytes to compress your data (default: 1024)
        /// </summary>
        public int CompressionThreshold { get => _compressionThreshold; set { CheckLocked(); _compressionThreshold = value; } }
        /// <summary>
        /// Compression level for compressed messages (default: Optimal)
        /// </summary>
        public CompressionLevel CompressionLevel { get => _compressionLevel; set { CheckLocked(); _compressionLevel = value; } }
        /// <summary>
        /// Context synchronization mode: Send ro Post (default: Post)
        /// </summary>
        public ContextSynchronizationMode ContextSynchronizationMode { get => _contextSynchronizationMode; set { CheckLocked(); _contextSynchronizationMode = value; } }

        private protected IMemoryManager _memoryManager;
        private protected ILogManager _logManager;
        
        CryptographyConfiguration _cryptographyConfiguration;
        ISessionFactory _sessionFactory;
        bool _orderedExecution;
        bool _logMessageBodyDebug;
        int _orderedExecutionMaxQueue;
        int _compressionThreshold;
        CompressionLevel _compressionLevel;
        TaskScheduler _taskScheduler;
        SynchronizationContext _synchronizationContext;
        RpcInvocationRules _rpcInvocationRules;
        ContextSynchronizationMode _contextSynchronizationMode;
        
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
            _synchronizationContext = SynchronizationContext.Current;
            _taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }
        
        public virtual void SetSynchronizationContext(SynchronizationContext context)
        {
            CheckLocked();
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            _synchronizationContext = context;
            _taskScheduler = new SynchronizationContextTaskScheduler(context);
        }
        
        internal void SynchronizeSafe(ILogger logger, string nameForLogs, SendOrPostCallback callback, object state)
        {
            ContextSynchronizationHelper.SynchronizeSafe(_synchronizationContext, _contextSynchronizationMode,
                logger, nameForLogs, callback, state);
        }

        public RpcConfiguration()
        {
            _cryptographyConfiguration.EncryptionAlgorithm = EncryptionAlgorithmEnum.Aes128;
            _cryptographyConfiguration.KeyExchangeAlgorithm = KeyExchangeAlgorithmEnum.Rsa2048;
            _compressionLevel = CompressionLevel.Optimal;
            _memoryManager = Util.Pooling.MemoryManager.Shared;
            _rpcInvocationRules = RpcInvocationRules.Default;
            _synchronizationContext = new NeonSynchronizationContext();
            _contextSynchronizationMode = ContextSynchronizationMode.Post;
            _taskScheduler = TaskScheduler.Default;
            _logManager = Logging.LogManager.Default;
            _orderedExecution = false;
            _orderedExecutionMaxQueue = 32;
            _compressionThreshold = 1024;
        }
    }
}