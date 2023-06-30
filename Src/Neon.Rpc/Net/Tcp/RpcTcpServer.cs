using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking.Tcp;
using Neon.Rpc.Net.Events;
using Neon.Util;
using ConnectionClosedEventArgs = Neon.Networking.Tcp.Events.ConnectionClosedEventArgs;
using ConnectionOpenedEventArgs = Neon.Networking.Tcp.Events.ConnectionOpenedEventArgs;

namespace Neon.Rpc.Net.Tcp
{
    public class RpcTcpServer
    {
        internal class InnerTcpServer : TcpServer
        {
            RpcTcpServer parent;
            RpcTcpConfigurationServer configuration;

            public InnerTcpServer(RpcTcpServer parent, RpcTcpConfigurationServer configuration) : base(configuration.TcpConfiguration)
            {
                this.configuration = configuration;
                this.parent = parent;
            }
            
            protected override TcpConnection CreateConnection()
            {
                return new RpcTcpConnection(this, configuration);
            }

            protected override void PollEvents()
            {
                base.PollEvents();
                parent.PollEvents();
            }
        }

        /// <summary>
        /// User-defined tag
        /// </summary>
        public string Tag { get; set; }
        /// <summary>
        /// Server configuration
        /// </summary>
        public RpcTcpConfigurationServer Configuration => configuration;

        readonly InnerTcpServer innerTcpServer;
        readonly RpcTcpConfigurationServer configuration;
        protected readonly ILogger logger;

        public RpcTcpServer(RpcTcpConfigurationServer configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (configuration.SessionFactory == null)
                throw new ArgumentNullException(nameof(configuration.SessionFactory));
            configuration.Lock();
            this.configuration = configuration;
            innerTcpServer = new InnerTcpServer(this, configuration);
            logger = configuration.LogManager.GetLogger(typeof(RpcTcpServer));
            logger.Meta["tag"] = new RefLogLabel<RpcTcpServer>(this, s => s.Tag);
        }

        /// <summary>
        /// The number of currently active sessions
        /// </summary>
        public int SessionsCount => innerTcpServer.Connections.Count;
        
        /// <summary>
        /// Thread-safe sessions enumerator
        /// </summary>
        public IEnumerable<RpcSessionBase> Sessions
        {
            get
            {
                foreach (var pair in innerTcpServer.Connections)
                {
                    var session = (pair.Value as RpcTcpConnection)?.UserSession;
                    if (session != null)
                        yield return session;
                }
            }
        }

        protected virtual void PollEvents()
        {
            
        }

        /// <summary>
        /// Start an internal network thread
        /// </summary>
        public void Start()
        {
            innerTcpServer.Start();
        }
        
        /// <summary>
        /// Shutting down the peer, destroying all the connections and free memory
        /// </summary>
        public void Shutdown()
        {
            innerTcpServer.Shutdown();
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
            innerTcpServer.Listen(port);
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
            innerTcpServer.Listen(host, port);
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
            innerTcpServer.Listen(endPoint);
        }

    }
}
