using Neon.Logging;
using Neon.Networking;
using Neon.Networking.Udp;
using Neon.Rpc.Authorization;
using Neon.Util.Pooling;

namespace Neon.Rpc.Net.Udp
{
    public class RpcUdpConfigurationServer : RpcConfiguration
    {
             public ConnectionSimulation ConnectionSimulation
        {
            get => udpConfiguration.ConnectionSimulation;
            set
            {
                CheckLocked();
                udpConfiguration.ConnectionSimulation = value;
            }
        }

        public int SendBufferSize
        {
            get => udpConfiguration.SendBufferSize;
            set
            {
                CheckLocked();
                udpConfiguration.SendBufferSize = value;
            }
        }

        public int ReceiveBufferSize
        {
            get => udpConfiguration.ReceiveBufferSize;
            set
            {
                CheckLocked();
                udpConfiguration.ReceiveBufferSize = value;
            }
        }

        public ILogManager NetworkLogManager
        {
            get => udpConfiguration.LogManager;
            set
            {
                CheckLocked();
                CheckNull(value);
                udpConfiguration.LogManager = value;
            }
        }

        public IMemoryManager MemoryManager
        {
            get => udpConfiguration.MemoryManager;
            set
            {
                CheckLocked();
                CheckNull(value);
                udpConfiguration.MemoryManager = value;
            }
        }

        public int KeepAliveInterval
        {
            get => udpConfiguration.KeepAliveInterval;
            set
            {
                CheckLocked();
                udpConfiguration.KeepAliveInterval = value;
            }
        }

        public int ConnectionTimeout
        {
            get => udpConfiguration.ConnectionTimeout;
            set
            {
                CheckLocked();
                udpConfiguration.ConnectionTimeout = value;
            }
        }
        
        public int LimitMtu
        {
            get => udpConfiguration.LimitMtu;
            set
            {
                CheckLocked();
                udpConfiguration.LimitMtu = value;
            }
        }

        public int ConnectionLingerTimeout
        {
            get => udpConfiguration.ConnectionLingerTimeout;
            set
            {
                CheckLocked();
                udpConfiguration.ConnectionLingerTimeout = value;
            }
        }
        
        public int NetworkReceiveThreads
        {
            get => udpConfiguration.NetworkReceiveThreads;
            set
            {
                CheckLocked();
                udpConfiguration.NetworkReceiveThreads = value;
            }
        }
        
        public bool ReuseAddress
        {
            get => udpConfiguration.ReuseAddress;
            set
            {
                CheckLocked();
                udpConfiguration.ReuseAddress = value;
            }
        }
        
        public IAuthSessionFactory AuthSessionFactory
        {
            get => authSessionFactory;
            set
            {
                CheckLocked();
                authSessionFactory = value;
            }
        }

        internal UdpConfigurationServer UdpConfiguration => udpConfiguration;
        UdpConfigurationServer udpConfiguration;
        protected IAuthSessionFactory authSessionFactory;

        public RpcUdpConfigurationServer() : base()
        {
            udpConfiguration = new UdpConfigurationServer();
            CompressionThreshold = UdpConnection.INITIAL_MTU;
        }
    }
}
