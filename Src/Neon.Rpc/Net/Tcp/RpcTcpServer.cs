using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Neon.Rpc.Net.Events;
using Neon.Logging;
using Neon.Networking.Tcp;
using Neon.Networking.Tcp.Events;
using Neon.Util;

namespace Neon.Rpc.Net.Tcp
{
    public class RpcTcpServer : IRpcPeer
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
                return new RpcTcpConnection(this, parent, configuration);
            }

            protected override void PollEvents()
            {
                base.PollEvents();
                parent.PollEvents();
            }

            protected override void OnConnectionClosed(Networking.Tcp.Events.ConnectionClosedEventArgs args)
            {
                base.OnConnectionClosed(args);
                parent.OnConnectionClosed(args);
            }

            protected override void OnConnectionOpened(Networking.Tcp.Events.ConnectionOpenedEventArgs args)
            {
                base.OnConnectionOpened(args);
                parent.OnConnectionOpened(args);
            }
        }

        public string Tag { get; set; }
        public event DOnSessionOpened OnSessionOpenedEvent;
        public event DOnSessionClosed OnSessionClosedEvent;
        public RpcTcpConfigurationServer Configuration => configuration;

        readonly InnerTcpServer innerTcpServer;
        readonly RpcTcpConfigurationServer configuration;
        protected readonly ILogger logger;

        public RpcTcpServer(RpcTcpConfigurationServer configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (configuration.Serializer == null)
                throw new ArgumentNullException(nameof(configuration.Serializer));
            if (configuration.SessionFactory == null)
                throw new ArgumentNullException(nameof(configuration.SessionFactory));
            configuration.Lock();
            this.configuration = configuration;
            this.innerTcpServer = new InnerTcpServer(this, configuration);
            this.logger = configuration.LogManager.GetLogger(nameof(RpcTcpServer));
            this.logger.Meta["kind"] = this.GetType().Name;
            this.logger.Meta["tag"] = new RefLogLabel<RpcTcpServer>(this, s => s.Tag);
        }

        public int SessionsCount => innerTcpServer.Connections.Count;
        public IEnumerable<RpcSession> Sessions
        {
            get
            {
                foreach (var connection in innerTcpServer.Connections.Values)
                {
                    var session = (connection as RpcTcpConnection)?.Session;
                    if (session != null)
                        yield return session;
                }
            }
        }

        protected virtual void PollEvents()
        {
            
        }

        public void Start()
        {
            innerTcpServer.Start();
        }
        
        public void Shutdown()
        {
            innerTcpServer.Shutdown();
        }
        
        public void Listen(int port)
        {
            innerTcpServer.Listen(port);
        }

        public void Listen(string host, int port)
        {
            innerTcpServer.Listen(host, port);
        }

        public void Listen(IPEndPoint endPoint)
        {
            innerTcpServer.Listen(endPoint);
        }

        internal void OnConnectionOpened(ConnectionOpenedEventArgs args)
        {
            RpcTcpConnection connection = (RpcTcpConnection)args.Connection;
            Start(connection).ContinueWith(t =>
            {
                if (!(t.Exception.GetInnermostException() is OperationCanceledException))
                    logger.Debug($"{connection} start failed with: {t.Exception.GetInnermostException()}");
                connection.Close();
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        
        async Task Start(RpcTcpConnection connection)
        {
            await connection.Start(default).ConfigureAwait(false);
            connection.StartServerSession();
        }

        internal void OnConnectionClosed(ConnectionClosedEventArgs args)
        {
            
        }

        void IRpcPeer.OnSessionOpened(SessionOpenedEventArgs args)
        {
            configuration.SynchronizeSafe(logger, $"{nameof(RpcTcpServer)}.{nameof(OnSessionOpened)}",
                (state) => OnSessionOpened(state as SessionOpenedEventArgs), args);
            configuration.SynchronizeSafe(logger, $"{nameof(RpcTcpServer)}.{nameof(OnSessionOpenedEvent)}",
                (state) => OnSessionOpenedEvent?.Invoke(state as SessionOpenedEventArgs), args);
        }

        void IRpcPeer.OnSessionClosed(SessionClosedEventArgs args)
        {
            configuration.SynchronizeSafe(logger, $"{nameof(RpcTcpServer)}.{nameof(OnSessionClosed)}",
                (state) => OnSessionClosed(state as SessionClosedEventArgs), args);
            configuration.SynchronizeSafe(logger, $"{nameof(RpcTcpServer)}.{nameof(OnSessionClosedEvent)}",
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
