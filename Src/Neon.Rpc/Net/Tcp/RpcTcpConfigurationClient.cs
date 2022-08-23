using System.Threading;
using Neon.Logging;
using Neon.Networking;
using Neon.Networking.Tcp;
using Neon.Rpc.Authorization;
using Neon.Util.Pooling;

namespace Neon.Rpc.Net.Tcp
{
    public class RpcTcpConfigurationClient : RpcConfiguration
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
        
        public int ConnectTimeout
        {
            get => tcpConfiguration.ConnectTimeout;
            set
            {
                CheckLocked();
                tcpConfiguration.ConnectTimeout = value;
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
        
        
        
        internal TcpConfigurationClient TcpConfiguration => tcpConfiguration;

        TcpConfigurationClient tcpConfiguration;

        public RpcTcpConfigurationClient() : base()
        {
            tcpConfiguration = new TcpConfigurationClient();
            tcpConfiguration.ConnectTimeout = Timeout.Infinite;
        }
    }
}
