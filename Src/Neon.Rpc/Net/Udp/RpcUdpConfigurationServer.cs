﻿using Neon.Logging;
using Neon.Networking;
using Neon.Networking.Udp;
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
        /// Log manager (default: LogManager.Default)
        /// </summary>
        public override ILogManager LogManager
        {
            get => base._logManager;
            set
            {
                CheckLocked();
                CheckNull(value);
                udpConfiguration.LogManager = value;
                base._logManager = value;
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
                udpConfiguration.MemoryManager = value;
                base._memoryManager = value;
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
        
        internal UdpConfigurationServer UdpConfiguration => udpConfiguration;
        UdpConfigurationServer udpConfiguration;

        public RpcUdpConfigurationServer()
        {
            udpConfiguration = new UdpConfigurationServer();
            udpConfiguration.ContextSynchronizationMode = ContextSynchronizationMode.Send;
            CompressionThreshold = UdpConnection.INITIAL_MTU;
        }
    }
}
