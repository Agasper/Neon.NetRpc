using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using Neon.Logging;
using Neon.Networking.Udp.Events;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp
{
    public class UdpServer : UdpPeer
    {
        public new UdpConfigurationServer Configuration { get; }

        public IReadOnlyDictionary<UdpNetEndpoint, UdpConnection> Connections => _connections;

        private protected override ILogger Logger => _logger;
        readonly ConcurrentDictionary<UdpNetEndpoint, UdpConnection> _connections;
        readonly IEnumerator<KeyValuePair<UdpNetEndpoint, UdpConnection>> _connectionsEnumerator;


        readonly ILogger _logger;

        public UdpServer(UdpConfigurationServer configuration) : base(configuration)
        {
            Configuration = configuration;
            _connections = new ConcurrentDictionary<UdpNetEndpoint, UdpConnection>();
            _connectionsEnumerator = _connections.GetEnumerator();
            _logger = configuration.LogManager.GetLogger(typeof(UdpServer));
        }

        public void Listen(int port)
        {
            CheckStarted();
            Bind(null, port);
            _logger.Info($"Listening on :{port}");
        }

        public void Listen(string ip, int port)
        {
            CheckStarted();
            Bind(ip, port);
            _logger.Info($"Listening on {ip}:{port}");
        }

        public void Listen(IPEndPoint endPoint)
        {
            CheckStarted();
            Bind(endPoint);
            _logger.Info($"Listening on {endPoint}");
        }

        public override void Shutdown()
        {
            base.Shutdown();
            foreach (UdpConnection connection in _connections.Values)
                connection.CloseImmediately(DisconnectReason.ClosedByThisPeer);
            _connections.Clear();
        }

        internal bool OnAcceptConnectionInternal(OnAcceptConnectionEventArgs args)
        {
            bool result = OnAcceptConnection(args);
            if (result)
                _logger.Debug($"Accepting connection from {args.Endpoint}");
            else
                _logger.Debug($"Rejecting connection from {args.Endpoint}");
            return result;
        }

        protected virtual bool OnAcceptConnection(OnAcceptConnectionEventArgs args)
        {
            return true;
        }

        private protected override void OnDatagram(Datagram datagram, UdpNetEndpoint remoteEndpoint)
        {
            var isNew = false;
            UdpConnection connection = _connections.GetOrAdd(remoteEndpoint, endpoint =>
            {
                isNew = true;
                return CreateConnection();
            });

            if (isNew)
            {
                _logger.Debug($"Connection added to the server {connection}");
                connection.Init(remoteEndpoint, false);
            }

            connection.OnDatagram(datagram);
        }

        private protected override void PollEventsInternal()
        {
            base.PollEventsInternal();

            _connectionsEnumerator.Reset();
            while (_connectionsEnumerator.MoveNext())
            {
                KeyValuePair<UdpNetEndpoint, UdpConnection> pair = _connectionsEnumerator.Current;

                try
                {
                    if (!pair.Value.PollEventsInternal())
                    {
                        if (!_connections.TryRemove(pair.Key, out _))
                            throw new InvalidOperationException($"Could not remove dead connection {pair.Value}");
                        _logger.Debug($"Connection removed from server {pair.Value} ({_connections.Count} left)");
                        pair.Value.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"{nameof(UdpConnection)}.PollEvents() got an unhandled exception: {ex}");
                    pair.Value.CloseImmediately(DisconnectReason.Error);
                }
            }
        }
    }
}