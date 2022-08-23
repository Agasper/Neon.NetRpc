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
            RpcUdpServer parent;

            public InnerUdpServer(RpcUdpServer parent, RpcUdpConfigurationServer configuration) : base(configuration.UdpConfiguration)
            {
                this.parent = parent;
            }
            
            protected override UdpConnection CreateConnection()
            {
                return parent.CreateConnection();
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

        public event DOnSessionOpened OnSessionOpenedEvent;
        public event DOnSessionClosed OnSessionClosedEvent;
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
            this.logger = configuration.LogManager.GetLogger(nameof(RpcUdpServer));
            this.logger.Meta["kind"] = this.GetType().Name;
        }

        public int SessionsCount => innerUdpServer.Connections.Count;

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

        public void Start()
        {
            innerUdpServer.Start();
        }
        
        public void Shutdown()
        {
            innerUdpServer.Shutdown();
        }
        
        public void Listen(int port)
        {
            innerUdpServer.Listen(port);
        }

        public void Listen(string host, int port)
        {
            innerUdpServer.Listen(host, port);
        }

        public void Listen(IPEndPoint endPoint)
        {
            innerUdpServer.Listen(endPoint);
        }

        internal virtual RpcUdpConnection CreateConnection()
        {
            RpcUdpConnection connection = new RpcUdpConnection(innerUdpServer, this, configuration);
            return connection;
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
