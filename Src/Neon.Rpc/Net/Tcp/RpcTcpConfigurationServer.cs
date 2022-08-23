using System.Net.Sockets;
using Neon.Logging;
using Neon.Networking;
using Neon.Networking.Tcp;
using Neon.Rpc.Authorization;
using Neon.Util.Pooling;

namespace Neon.Rpc.Net.Tcp
{
    public class RpcTcpConfigurationServer : RpcConfiguration
    {
        public ConnectionSimulation ConnectionSimulation
        {
            get => tcpConfiguration.ConnectionSimulation;
            set
            {
                CheckLocked();
                tcpConfiguration.ConnectionSimulation = value;
            }
        }

        public int SendBufferSize
        {
            get => tcpConfiguration.SendBufferSize;
            set
            {
                CheckLocked();
                tcpConfiguration.SendBufferSize = value;
            }
        }

        public int ReceiveBufferSize
        {
            get => tcpConfiguration.ReceiveBufferSize;
            set
            {
                CheckLocked();
                tcpConfiguration.ReceiveBufferSize = value;
            }
        }

        public ILogManager LogManagerNetwork
        {
            get => tcpConfiguration.LogManager;
            set
            {
                CheckLocked();
                CheckNull(value);
                tcpConfiguration.LogManager = value;
            }
        }

        public IMemoryManager MemoryManager
        {
            get => tcpConfiguration.MemoryManager;
            set
            {
                CheckLocked();
                CheckNull(value);
                tcpConfiguration.MemoryManager = value;
            }
        }

        public bool KeepAliveEnabled
        {
            get => tcpConfiguration.KeepAliveEnabled;
            set
            {
                CheckLocked();
                tcpConfiguration.KeepAliveEnabled = value;
            }
        }

        public int KeepAliveInterval
        {
            get => tcpConfiguration.KeepAliveInterval;
            set
            {
                CheckLocked();
                tcpConfiguration.KeepAliveInterval = value;
            }
        }

        public int KeepAliveTimeout
        {
            get => tcpConfiguration.KeepAliveTimeout;
            set
            {
                CheckLocked();
                tcpConfiguration.KeepAliveTimeout = value;
            }
        }
        
        
        public int AcceptThreads
        {
            get => tcpConfiguration.AcceptThreads;
            set
            {
                CheckLocked();
                tcpConfiguration.AcceptThreads = value;
            }
        }
        
        public int ListenBacklog
        {
            get => tcpConfiguration.ListenBacklog;
            set
            {
                CheckLocked();
                tcpConfiguration.ListenBacklog = value;
            }
        }
        
        public LingerOption LingerOption
        {
            get => tcpConfiguration.LingerOption;
            set
            {
                CheckLocked();
                tcpConfiguration.LingerOption = value;
            }
        }
        
                
        public bool NoDelay
        {
            get => tcpConfiguration.NoDelay;
            set
            {
                CheckLocked();
                tcpConfiguration.NoDelay = value;
            }
        }
        
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
        
        
        public IAuthSessionFactory AuthSessionFactory
        {
            get => authSessionFactory;
            set
            {
                CheckLocked();
                authSessionFactory = value;
            }
        }

        TcpConfigurationServer tcpConfiguration;
        protected IAuthSessionFactory authSessionFactory;

        public RpcTcpConfigurationServer() : base()
        {
            tcpConfiguration = new TcpConfigurationServer();
        }
    }
}
