using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Neon.Networking.Udp.Messages;
using Neon.Logging;
using Neon.Networking.Udp.Events;

namespace Neon.Networking.Udp
{
    public class UdpServer : UdpPeer
    {
        public new UdpConfigurationServer Configuration => configuration;
        public IReadOnlyDictionary<UdpNetEndpoint, UdpConnection> Connections
        {
            get
            {
                return connections;
            }
        }
        UdpConfigurationServer configuration;

        private protected override ILogger Logger => logger;


        ILogger logger;
        ConcurrentDictionary<UdpNetEndpoint, UdpConnection> connections;
        readonly IEnumerator<KeyValuePair<UdpNetEndpoint, UdpConnection>> connectionsEnumerator;

        public UdpServer(UdpConfigurationServer configuration) : base(configuration)
        {
            this.configuration = configuration;
            this.connections = new ConcurrentDictionary<UdpNetEndpoint, UdpConnection>();
            this.connectionsEnumerator = this.connections.GetEnumerator();
            this.logger = configuration.LogManager.GetLogger(typeof(UdpServer));
            this.logger.Meta["kind"] = this.GetType().Name;
        }
        
        public void Listen(int port)
        {
            CheckStarted();
            Bind(null, port);
            this.logger.Info($"Listening on :{port}");
        }

        public void Listen(string ip, int port)
        {
            CheckStarted();
            Bind(ip, port);
            this.logger.Info($"Listening on {ip}:{port}");
        }

        public void Listen(IPEndPoint endPoint)
        {
            CheckStarted();
            Bind(endPoint);
            this.logger.Info($"Listening on {endPoint}");
        }

        public override void Shutdown()
        {
            base.Shutdown();
            foreach(var connection in connections.Values)
            {
                connection.CloseImmediately(DisconnectReason.ClosedByThisPeer);
            }
            connections.Clear();
        }
        
        internal bool OnAcceptConnectionInternal(OnAcceptConnectionEventArgs args)
        {
            bool result = OnAcceptConnection(args);
            if (result)
                logger.Debug($"Accepting connection from {args.Endpoint}");
            else
                logger.Debug($"Rejecting connection from {args.Endpoint}");
            return result;
        }
        
        protected virtual bool OnAcceptConnection(OnAcceptConnectionEventArgs args)
        {
            return true;
        }

        private protected override void OnDatagram(Datagram datagram, UdpNetEndpoint remoteEndpoint)
        {
            bool isNew = false;
            UdpConnection connection = this.connections.GetOrAdd(remoteEndpoint, endpoint =>
            {
                isNew = true;
                return this.CreateConnection();
            });

            if (isNew)
            {
                logger.Debug($"Connection added to the server {connection}");
                connection.Init(remoteEndpoint, false);
            }

            connection.OnDatagram(datagram);
        }

        private protected override void PollEventsInternal()
        {
            base.PollEventsInternal();
            
            connectionsEnumerator.Reset();
            while (connectionsEnumerator.MoveNext())
            {
                var pair = connectionsEnumerator.Current;

                try
                {
                    if (!pair.Value.PollEvents())
                    {
                        if (!connections.TryRemove(pair.Key, out _))
                            throw new InvalidOperationException($"Could not remove dead connection {pair.Value}");
                        logger.Debug($"Connection removed from server {pair.Value} ({connections.Count} left)");
                        pair.Value.Dispose();
                    }
                }
                catch(Exception ex)
                {
                    logger.Error($"{nameof(UdpConnection)}.PollEvents() got an unhandled exception: {ex}");
                    pair.Value.CloseImmediately(DisconnectReason.Error);
                }
            }

        }
    }
}
