using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Neon.Rpc.Net.Events;
using Neon.Logging;
using Neon.Networking.Udp;
using Neon.Networking.Udp.Events;
using Neon.Util;

namespace Neon.Rpc.Net.Udp
{
    public class RpcUdpServer : IRpcPeer
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
                return new RpcUdpConnection(this, parent, configuration);
            }

            protected override void OnConnectionClosed(ConnectionClosedEventArgs args)
            {
                base.OnConnectionClosed(args);
                parent.OnConnectionClosed(args);
            }

            protected override void OnConnectionOpened(ConnectionOpenedEventArgs args)
            {
                base.OnConnectionOpened(args);
                parent.OnConnectionOpened(args);
            }
        }

        /// <summary>
        /// User-defined tag
        /// </summary>
        public string Tag { get; set; }
        /// <summary>
        /// Raised when a new session is opened
        /// </summary>
        public event DOnSessionOpened OnSessionOpenedEvent;
        /// <summary>
        /// Raised if previously opened session closed
        /// </summary>
        public event DOnSessionClosed OnSessionClosedEvent;
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
            if (configuration.Serializer == null)
                throw new ArgumentNullException(nameof(configuration.Serializer));
            configuration.Lock();
            this.configuration = configuration;
            this.innerUdpServer = new InnerUdpServer(this, configuration);
            this.logger = configuration.LogManager.GetLogger(typeof(RpcUdpServer));
            this.logger.Meta["kind"] = this.GetType().Name;
        }

        /// <summary>
        /// The number of currently active sessions
        /// </summary>
        public int SessionsCount => innerUdpServer.Connections.Count;

        /// <summary>
        /// Thread-safe sessions enumerator
        /// </summary>
        public IEnumerable<RpcSession> Sessions
        {
            get
            {
                foreach (var connection in innerUdpServer.Connections.Values)
                {
                    var session = (connection as RpcUdpConnection)?.Session;
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

        internal void OnConnectionOpened(ConnectionOpenedEventArgs args)
        {
            RpcUdpConnection connection = (RpcUdpConnection)args.Connection;
            Start(connection).ContinueWith(t =>
            {
                logger.Debug($"{connection} start failed with: {t.Exception.GetInnermostException()}");
                connection.Close();
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        
                
        async Task Start(RpcUdpConnection connection)
        {
            await connection.Start(default).ConfigureAwait(false);
            connection.StartServerSession();
        }

        internal void OnConnectionClosed(ConnectionClosedEventArgs args)
        {

        }

        void IRpcPeer.OnSessionOpened(SessionOpenedEventArgs args)
        {
            configuration.SynchronizeSafe(logger, $"{nameof(RpcUdpServer)}.{nameof(OnSessionOpened)}",
                (state) => OnSessionOpened(state as SessionOpenedEventArgs), args);
            configuration.SynchronizeSafe(logger, $"{nameof(RpcUdpServer)}.{nameof(OnSessionOpenedEvent)}",
                (state) => OnSessionOpenedEvent?.Invoke(state as SessionOpenedEventArgs), args);
        }

        void IRpcPeer.OnSessionClosed(SessionClosedEventArgs args)
        {
            configuration.SynchronizeSafe(logger, $"{nameof(RpcUdpServer)}.{nameof(OnSessionClosed)}",
                (state) => OnSessionClosed(state as SessionClosedEventArgs), args);
            configuration.SynchronizeSafe(logger, $"{nameof(RpcUdpServer)}.{nameof(OnSessionClosedEvent)}",
                (state) => OnSessionClosedEvent?.Invoke(state as SessionClosedEventArgs), args);
        }

        protected virtual void OnSessionOpened(SessionOpenedEventArgs args)
        {

        }


        protected virtual void OnSessionClosed(SessionClosedEventArgs args)
        {
            
        }
        
    }
}
