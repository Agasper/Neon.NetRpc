using Neon.Logging;
using Neon.Networking;
using Neon.Networking.Udp;
using Neon.Rpc.Authorization;
using Neon.Util.Pooling;

namespace Neon.Rpc.Net.Udp
{
    public class RpcUdpConfigurationServer : RpcConfiguration
    {
        /// <summary>
        /// Allows you to simulate bad network behaviour
        /// </summary>
        public ConnectionSimulation ConnectionSimulation
        {
            get => udpConfiguration.ConnectionSimulation;
            set
            {
                CheckLocked();
                udpConfiguration.ConnectionSimulation = value;
            }
        }

        /// <summary>
        /// Set socket send buffer size
        /// </summary>
        public int SendBufferSize
        {
            get => udpConfiguration.SendBufferSize;
            set
            {
                CheckLocked();
                udpConfiguration.SendBufferSize = value;
            }
        }

        /// <summary>
        /// Set socket receive buffer size
        /// </summary>
        public int ReceiveBufferSize
        {
            get => udpConfiguration.ReceiveBufferSize;
            set
            {
                CheckLocked();
                udpConfiguration.ReceiveBufferSize = value;
            }
        }

        /// <summary>
        /// Log manager for network logs
        /// </summary>
        public ILogManager LogManagerNetwork
        {
            get => udpConfiguration.LogManager;
            set
            {
                CheckLocked();
                CheckNull(value);
                udpConfiguration.LogManager = value;
            }
        }

        /// <summary>
        /// A manager who provide us streams and arrays for a temporary use
        /// </summary>
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
        
        /// <summary>
        /// Interval for pings
        /// </summary>
        public int KeepAliveInterval
        {
            get => udpConfiguration.KeepAliveInterval;
            set
            {
                CheckLocked();
                udpConfiguration.KeepAliveInterval = value;
            }
        }

        /// <summary>
        /// If no packets received within this timeout, connection considered dead
        /// </summary>
        public int ConnectionTimeout
        {
            get => udpConfiguration.ConnectionTimeout;
            set
            {
                CheckLocked();
                udpConfiguration.ConnectionTimeout = value;
            }
        }
        
        /// <summary>
        /// Do not expand MTU greater than this value
        /// </summary>
        public int LimitMtu
        {
            get => udpConfiguration.LimitMtu;
            set
            {
                CheckLocked();
                udpConfiguration.LimitMtu = value;
            }
        }

        /// <summary>
        /// Connection will be destroyed after linger timeout
        /// </summary>
        public int ConnectionLingerTimeout
        {
            get => udpConfiguration.ConnectionLingerTimeout;
            set
            {
                CheckLocked();
                udpConfiguration.ConnectionLingerTimeout = value;
            }
        }
        
        /// <summary>
        /// The amount of threads for grabbing packets from the socket
        /// </summary>
        public int NetworkReceiveThreads
        {
            get => udpConfiguration.NetworkReceiveThreads;
            set
            {
                CheckLocked();
                udpConfiguration.NetworkReceiveThreads = value;
            }
        }
        
        /// <summary>
        /// Set SocketOptionName.ReuseAddress
        /// </summary>
        public bool ReuseAddress
        {
            get => udpConfiguration.ReuseAddress;
            set
            {
                CheckLocked();
                udpConfiguration.ReuseAddress = value;
            }
        }
        
        /// <summary>
        /// Sets the authentication for processing authentication. If set server will require client authentication
        /// </summary>
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
