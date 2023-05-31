using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Neon.Logging;

namespace Neon.Networking.Tcp
{
    public class TcpServer : TcpPeer
    {
        /// <summary>
        /// Thread-safe dictionary of current active connections
        /// </summary>
        public IReadOnlyDictionary<long, TcpConnection> Connections => connections;

        private protected override ILogger Logger => logger;

        readonly ILogger logger;
        readonly ConcurrentDictionary<long, TcpConnection> connections;
        readonly IEnumerator<KeyValuePair<long, TcpConnection>> connectionsEnumerator;
        
        new readonly TcpConfigurationServer configuration;
        
        long connectionId = 0;
        bool listening;
        Socket serverSocket;

        public TcpServer(TcpConfigurationServer configuration) : base(configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            this.connections = new ConcurrentDictionary<long, TcpConnection>(
                configuration.AcceptThreads / 2 + 1,
                Math.Min(configuration.MaximumConnections, 101));
            this.connectionsEnumerator = this.connections.GetEnumerator();
            this.configuration = configuration;
            this.logger = configuration.LogManager.GetLogger(typeof(TcpServer));
            this.logger.Meta.Add("kind", this.GetType().Name);
        }

        /// <summary>
        /// Shutting down the peer, destroying all the connections and free memory
        /// </summary>
        public override void Shutdown()
        {
            if (serverSocket == null)
                return;

            logger.Info($"{nameof(TcpServer)} shutdown");

            listening = false;

            serverSocket.Close();
            serverSocket.Dispose();

            ClearConnections();

            base.Shutdown();
        }

        void ClearConnections()
        {
            foreach (var pair in connections)
            {
                pair.Value.Close();
            }
            connections.Clear();
        }

        private protected override void PollEventsInternal()
        {
            connectionsEnumerator.Reset();
            while (connectionsEnumerator.MoveNext())
            {
                var pair = connectionsEnumerator.Current;
                if (pair.Value.Connected)
                    pair.Value.PollEventsInternal();
            }
        }

        internal override void OnConnectionClosedInternal(TcpConnection tcpConnection, Exception ex)
        {
            if (!connections.TryRemove(tcpConnection.Id, out _))
            {
                logger.Error($"Couldn't remove connection from server {tcpConnection}. It doesn't exists");
            }
            else
                logger.Debug($"{tcpConnection} was removed from server ({connections.Count} left)");

            base.OnConnectionClosedInternal(tcpConnection, ex);
        }

        /// <summary>
        /// Places a peer in a listening state
        /// </summary>
        /// <param name="port">A port the socket will bind to</param>
        /// <exception cref="T:System.Net.Sockets.SocketException">An error occurred when attempting to access the socket.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.Net.Sockets.Socket" /> has been closed.</exception>
        /// <exception cref="T:System.Security.SecurityException">A caller higher in the call stack does not have permission for the requested operation.</exception>
        public void Listen(int port)
        {
            Listen(null, port);
        }

        /// <summary>
        /// Places a peer in a listening state
        /// </summary>
        /// <param name="host">An ip address or domain the socket will bind to. Can be null, it wil use a default one</param>
        /// <param name="port">A port the socket will bind to</param>
        /// <exception cref="T:System.Net.Sockets.SocketException">An error occurred when attempting to access the socket.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.Net.Sockets.Socket" /> has been closed.</exception>
        /// <exception cref="T:System.Security.SecurityException">A caller higher in the call stack does not have permission for the requested operation.</exception>
        public void Listen(string host, int port)
        {
            IPEndPoint myEndpoint;
            if (string.IsNullOrEmpty(host))
                myEndpoint = new IPEndPoint(IPAddress.Any, port);
            else
                myEndpoint = new IPEndPoint(IPAddress.Parse(host), port);

            Listen(myEndpoint);
        }

        /// <summary>
        /// Places a peer in a listening state
        /// </summary>
        /// <param name="endPoint">An ip endpoint the socket will bind to</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="endPoint" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.Net.Sockets.SocketException">An error occurred when attempting to access the socket.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.Net.Sockets.Socket" /> has been closed.</exception>
        /// <exception cref="T:System.Security.SecurityException">A caller higher in the call stack does not have permission for the requested operation.</exception>
        public void Listen(IPEndPoint endPoint)
        {
            CheckStarted();
            if (serverSocket != null)
                throw new InvalidOperationException("Server already listening");

            serverSocket = GetNewSocket(endPoint.AddressFamily);
            //https://stackoverflow.com/questions/14388706/how-do-so-reuseaddr-and-so-reuseport-differ
            serverSocket.Bind(endPoint);
            serverSocket.Listen(configuration.ListenBacklog);

            for (int i = 0; i < configuration.AcceptThreads; i++)
            {
                StartAccept();
            }

            listening = true;

            logger.Debug($"Listening on {endPoint}");
        }

        void StartAccept()
        {
            if (serverSocket == null)
                return;

            serverSocket.BeginAccept(AcceptCallback, null);
        }

        protected virtual bool OnAcceptConnection(Socket socket)
        {
            return true;
        }

        void AcceptCallback(IAsyncResult asyncResult)
        {
            try
            {
                if (listening)
                    StartAccept();
                
                Socket connSocket = serverSocket.EndAccept(asyncResult);
                logger.Debug("Accepting new connection...");
                
                if (connections.Count >= configuration.MaximumConnections)
                {
                    logger.Warn("New connection rejected due to max connection limit");
                    connSocket.Close();
                    return;
                }

                if (!listening || !OnAcceptConnection(connSocket))
                {
                    logger.Debug("Server rejected new connection");
                    connSocket.Close();
                    return;
                }


                // Finish Accept
                SetSocketOptions(connSocket);
                long newId = Interlocked.Increment(ref connectionId);
                TcpConnection connection = this.CreateConnection();
                connection.CheckParent(this);
                connection.Init(newId, connSocket, false);

                if (!connections.TryAdd(newId, connection))
                {
                    logger.Error("New connection rejected. Blocking collection reject addition.");
                    connection.Dispose();
                    return;
                }
                else
                {
                    logger.Debug($"{connection} was added to the server");
                }

                connection.StartReceive();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                logger.Error($"{nameof(TcpServer)} encountered exception on accepting thread: {ex}");
            }
        }
    }
}
