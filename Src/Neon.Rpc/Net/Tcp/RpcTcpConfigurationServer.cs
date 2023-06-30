using System.Net.Sockets;
using Neon.Logging;
using Neon.Networking;
using Neon.Networking.Tcp;
using Neon.Util.Pooling;

namespace Neon.Rpc.Net.Tcp
{
    public class RpcTcpConfigurationServer : RpcConfiguration
    {
        /// <summary>
        /// Allows you to simulate bad network behaviour. Packet loss applied only to UDP
        /// </summary>
        public ConnectionSimulation ConnectionSimulation
        {
            get => tcpConfiguration.ConnectionSimulation;
            set
            {
                CheckLocked();
                tcpConfiguration.ConnectionSimulation = value;
            }
        }

        /// <summary>
        /// Sets socket send buffer size
        /// </summary>
        public int SendBufferSize
        {
            get => tcpConfiguration.SendBufferSize;
            set
            {
                CheckLocked();
                tcpConfiguration.SendBufferSize = value;
            }
        }

        /// <summary>
        /// Sets socket receive buffer size
        /// </summary>
        public int ReceiveBufferSize
        {
            get => tcpConfiguration.ReceiveBufferSize;
            set
            {
                CheckLocked();
                tcpConfiguration.ReceiveBufferSize = value;
            }
        }

        /// <summary>
        /// A manager which provide us streams and arrays for a temporary use
        /// </summary>
        public override IMemoryManager MemoryManager
        {
            get => base._memoryManager;
            set
            {
                CheckLocked();
                CheckNull(value);
                tcpConfiguration.MemoryManager = value;
                base._memoryManager = value;
            }
        }
        
        /// <summary>
        /// Log manager (default: LogManager.Default)
        /// </summary>
        public override ILogManager LogManager
        {
            get => base._logManager;
            set
            {
                CheckLocked();
                CheckNull(value);
                tcpConfiguration.LogManager = value;
                base._logManager = value;
            }
        }

        /// <summary>
        /// If true we will send ping packets to the other peer every KeepAliveInterval
        /// </summary>
        public bool KeepAliveEnabled
        {
            get => tcpConfiguration.KeepAliveEnabled;
            set
            {
                CheckLocked();
                tcpConfiguration.KeepAliveEnabled = value;
            }
        }

        /// <summary>
        /// Interval for pings
        /// </summary>
        public int KeepAliveInterval
        {
            get => tcpConfiguration.KeepAliveInterval;
            set
            {
                CheckLocked();
                tcpConfiguration.KeepAliveInterval = value;
            }
        }

        /// <summary>
        /// If we haven't got response for ping within timeout, we drop the connection
        /// </summary>
        public int KeepAliveTimeout
        {
            get => tcpConfiguration.KeepAliveTimeout;
            set
            {
                CheckLocked();
                tcpConfiguration.KeepAliveTimeout = value;
            }
        }
        
        /// <summary>
        /// The amount of threads who accepting connections
        /// </summary>
        public int AcceptThreads
        {
            get => tcpConfiguration.AcceptThreads;
            set
            {
                CheckLocked();
                tcpConfiguration.AcceptThreads = value;
            }
        }
        
        /// <summary>
        /// The maximum length of the pending connections queue
        /// </summary>
        public int ListenBacklog
        {
            get => tcpConfiguration.ListenBacklog;
            set
            {
                CheckLocked();
                tcpConfiguration.ListenBacklog = value;
            }
        }
        
        /// <summary>
        /// Sets the socket linger options
        /// </summary>
        public LingerOption LingerOption
        {
            get => tcpConfiguration.LingerOption;
            set
            {
                CheckLocked();
                tcpConfiguration.LingerOption = value;
            }
        }
        
        /// <summary>
        /// Sets a value that specifies whether the socket is using the Nagle algorithm
        /// </summary>
        public bool NoDelay
        {
            get => tcpConfiguration.NoDelay;
            set
            {
                CheckLocked();
                tcpConfiguration.NoDelay = value;
            }
        }
        
        /// <summary>
        /// Sets SocketOptionName.ReuseAddress
        /// </summary>
        public bool ReuseAddress
        {
            get => tcpConfiguration.ReuseAddress;
            set
            {
                CheckLocked();
                tcpConfiguration.ReuseAddress = value;
            }
        }

        internal TcpConfigurationServer TcpConfiguration => tcpConfiguration;
        
        TcpConfigurationServer tcpConfiguration;

        public RpcTcpConfigurationServer()
        {
            tcpConfiguration = new TcpConfigurationServer();
            tcpConfiguration.ContextSynchronizationMode = ContextSynchronizationMode.Send;
            tcpConfiguration.MemoryManager = base._memoryManager;
            tcpConfiguration.LogManager = base._logManager;
        }
    }
}
