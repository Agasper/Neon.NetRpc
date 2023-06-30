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
        ///     Thread-safe dictionary of current active connections
        /// </summary>
        public IReadOnlyDictionary<long, TcpConnection> Connections => _connections;

        private protected override ILogger Logger => _logger;
        new readonly TcpConfigurationServer _configuration;
        readonly ConcurrentDictionary<long, TcpConnection> _connections;
        readonly IEnumerator<KeyValuePair<long, TcpConnection>> _connectionsEnumerator;

        readonly ILogger _logger;

        long _connectionId;
        bool _listening;
        Socket _serverSocket;

        public TcpServer(TcpConfigurationServer configuration) : base(configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            _connections = new ConcurrentDictionary<long, TcpConnection>(
                configuration.AcceptThreads / 2 + 1,
                Math.Min(configuration.MaximumConnections, 101));
            _connectionsEnumerator = _connections.GetEnumerator();
            _configuration = configuration;
            _logger = configuration.LogManager.GetLogger(typeof(TcpServer));
        }

        /// <summary>
        ///     Shutting down the peer, destroying all the connections and free memory
        /// </summary>
        public override void Shutdown()
        {
            if (_serverSocket == null)
                return;

            _logger.Info($"{nameof(TcpServer)} shutdown");

            _listening = false;

            _serverSocket.Close();
            _serverSocket.Dispose();

            ClearConnections();

            base.Shutdown();
        }

        void ClearConnections()
        {
            foreach (KeyValuePair<long, TcpConnection> pair in _connections) pair.Value.Close();
            _connections.Clear();
        }

        private protected override void PollEventsInternal()
        {
            _connectionsEnumerator.Reset();
            while (_connectionsEnumerator.MoveNext())
            {
                KeyValuePair<long, TcpConnection> pair = _connectionsEnumerator.Current;
                if (pair.Value.Connected)
                    pair.Value.PollEvents();
            }
        }

        internal override void OnConnectionClosedInternal(TcpConnection tcpConnection, Exception ex)
        {
            if (!_connections.TryRemove(tcpConnection.Id, out _))
                _logger.Error($"Couldn't remove connection from server {tcpConnection}. It doesn't exists");
            else
                _logger.Debug($"{tcpConnection} was removed from server ({_connections.Count} left)");

            base.OnConnectionClosedInternal(tcpConnection, ex);
        }

        /// <summary>
        ///     Places a peer in a listening state
        /// </summary>
        /// <param name="port">A port the socket will bind to</param>
        /// <exception cref="T:System.Net.Sockets.SocketException">An error occurred when attempting to access the socket.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.Net.Sockets.Socket" /> has been closed.</exception>
        /// <exception cref="T:System.Security.SecurityException">
        ///     A caller higher in the call stack does not have permission for
        ///     the requested operation.
        /// </exception>
        public void Listen(int port)
        {
            Listen(null, port);
        }

        /// <summary>
        ///     Places a peer in a listening state
        /// </summary>
        /// <param name="host">An ip address or domain the socket will bind to. Can be null, it wil use a default one</param>
        /// <param name="port">A port the socket will bind to</param>
        /// <exception cref="T:System.Net.Sockets.SocketException">An error occurred when attempting to access the socket.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.Net.Sockets.Socket" /> has been closed.</exception>
        /// <exception cref="T:System.Security.SecurityException">
        ///     A caller higher in the call stack does not have permission for
        ///     the requested operation.
        /// </exception>
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
        ///     Places a peer in a listening state
        /// </summary>
        /// <param name="endPoint">An ip endpoint the socket will bind to</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="endPoint" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.Net.Sockets.SocketException">An error occurred when attempting to access the socket.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.Net.Sockets.Socket" /> has been closed.</exception>
        /// <exception cref="T:System.Security.SecurityException">
        ///     A caller higher in the call stack does not have permission for
        ///     the requested operation.
        /// </exception>
        public void Listen(IPEndPoint endPoint)
        {
            CheckStarted();
            if (_serverSocket != null)
                throw new InvalidOperationException("Server already listening");

            _serverSocket = GetNewSocket(endPoint.AddressFamily);
            //https://stackoverflow.com/questions/14388706/how-do-so-reuseaddr-and-so-reuseport-differ
            _serverSocket.Bind(endPoint);
            _serverSocket.Listen(_configuration.ListenBacklog);

            for (var i = 0; i < _configuration.AcceptThreads; i++) StartAccept();

            _listening = true;

            _logger.Debug($"Listening on {endPoint}");
        }

        void StartAccept()
        {
            if (_serverSocket == null)
                return;

            _serverSocket.BeginAccept(AcceptCallback, null);
        }

        protected virtual bool OnAcceptConnection(Socket socket)
        {
            return true;
        }

        void AcceptCallback(IAsyncResult asyncResult)
        {
            try
            {
                if (_listening)
                    StartAccept();

                Socket connSocket = _serverSocket.EndAccept(asyncResult);
                _logger.Debug("Accepting new connection...");

                if (_connections.Count >= _configuration.MaximumConnections)
                {
                    _logger.Warn("New connection rejected due to max connection limit");
                    connSocket.Close();
                    return;
                }

                if (!_listening || !OnAcceptConnection(connSocket))
                {
                    _logger.Debug("Server rejected new connection");
                    connSocket.Close();
                    return;
                }


                // Finish Accept
                SetSocketOptions(connSocket);
                long newId = Interlocked.Increment(ref _connectionId);
                TcpConnection connection = CreateConnection();
                connection.CheckParent(this);
                connection.Init(newId, connSocket, false);

                if (!_connections.TryAdd(newId, connection))
                {
                    _logger.Error("New connection rejected. Blocking collection reject addition.");
                    connection.Dispose();
                    return;
                }

                _logger.Debug($"{connection} was added to the server");

                connection.StartReceive();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(TcpServer)} encountered exception on accepting thread: {ex}");
            }
        }
    }
}