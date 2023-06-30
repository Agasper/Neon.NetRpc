using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking.Udp;
using Neon.Rpc.Net.Events;
using Neon.Util;
using ConnectionClosedEventArgs = Neon.Networking.Udp.Events.ConnectionClosedEventArgs;
using ConnectionOpenedEventArgs = Neon.Networking.Udp.Events.ConnectionOpenedEventArgs;

namespace Neon.Rpc.Net.Udp
{
    public class RpcUdpServer
    {
        internal class InnerUdpServer : UdpServer
        {
            readonly RpcUdpServer parent;
            readonly RpcUdpConfigurationServer configuration;

            public InnerUdpServer(RpcUdpServer parent, RpcUdpConfigurationServer configuration) : base(configuration.UdpConfiguration)
            {
                this.parent = parent;
                this.configuration = configuration;
            }

            protected override UdpConnection CreateConnection()
            {
                return new RpcUdpConnection(this, configuration);
            }

        }

        /// <summary>
        /// User-defined tag
        /// </summary>
        public string Tag { get; set; }
        /// <summary>
        /// Server configuration
        /// </summary>
        public RpcUdpConfigurationServer Configuration => configuration;

        readonly InnerUdpServer innerUdpServer;
        readonly RpcUdpConfigurationServer configuration;
        protected readonly ILogger logger;

        public RpcUdpServer(RpcUdpConfigurationServer configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (configuration.SessionFactory == null)
                throw new ArgumentNullException(nameof(configuration.SessionFactory));
            configuration.Lock();
            this.configuration = configuration;
            innerUdpServer = new InnerUdpServer(this, configuration);
            logger = configuration.LogManager.GetLogger(typeof(RpcUdpServer));
        }

        /// <summary>
        /// The number of currently active sessions
        /// </summary>
        public int SessionsCount => innerUdpServer.Connections.Count;

        /// <summary>
        /// Thread-safe sessions enumerator
        /// </summary>
        public IEnumerable<RpcSessionBase> Sessions
        {
            get
            {
                foreach (var pair in innerUdpServer.Connections)
                {
                    var session = (pair.Value as RpcUdpConnection)?.UserSession;
                    if (session != null)
                        yield return session;
                }
            }
        }

        /// <summary>
        /// Start an internal network thread
        /// </summary>
        public void Start()
        {
            innerUdpServer.Start();
        }
        
        /// <summary>
        /// Shutting down the peer, destroying all the connections and free memory
        /// </summary>
        public void Shutdown()
        {
            innerUdpServer.Shutdown();
        }
        
        /// <summary>
        /// Places a server in a listening state
        /// </summary>
        /// <param name="port">A port the socket will bind to</param>
        /// <exception cref="T:System.Net.Sockets.SocketException">An error occurred when attempting to access the socket.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.Net.Sockets.Socket" /> has been closed.</exception>
        /// <exception cref="T:System.Security.SecurityException">A caller higher in the call stack does not have permission for the requested operation.</exception>
        public void Listen(int port)
        {
            innerUdpServer.Listen(port);
        }

        /// <summary>
        /// Places a server in a listening state
        /// </summary>
        /// <param name="host">An ip address or domain the socket will bind to. Can be null, it wil use a default one</param>
        /// <param name="port">A port the socket will bind to</param>
        /// <exception cref="T:System.Net.Sockets.SocketException">An error occurred when attempting to access the socket.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.Net.Sockets.Socket" /> has been closed.</exception>
        /// <exception cref="T:System.Security.SecurityException">A caller higher in the call stack does not have permission for the requested operation.</exception>
        public void Listen(string host, int port)
        {
            innerUdpServer.Listen(host, port);
        }

        /// <summary>
        /// Places a server in a listening state
        /// </summary>
        /// <param name="endPoint">An ip endpoint the socket will bind to</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="endPoint" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.Net.Sockets.SocketException">An error occurred when attempting to access the socket.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.Net.Sockets.Socket" /> has been closed.</exception>
        /// <exception cref="T:System.Security.SecurityException">A caller higher in the call stack does not have permission for the requested operation.</exception>
        public void Listen(IPEndPoint endPoint)
        {
            innerUdpServer.Listen(endPoint);
        }
    }
}
