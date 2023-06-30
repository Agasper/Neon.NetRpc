using System;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking;
using Neon.Networking.Messages;
using Neon.Networking.Tcp;
using Neon.Networking.Tcp.Events;
using Neon.Networking.Udp.Messages;
using Neon.Rpc.Controllers;

namespace Neon.Rpc.Net.Tcp
{
    class RpcTcpConnection : TcpConnection, IRpcConnectionInternal
    {
        /// <summary>
        /// Connection statistics
        /// </summary>
        IConnectionStatistics IRpcConnection.Statistics => Statistics;
        /// <summary>
        /// User session
        /// </summary>
        public RpcSessionBase UserSession => _controller.UserSession;
        internal RpcController Controller => _controller;

        readonly RpcController _controller;
        readonly RpcConfiguration _configuration;
        new readonly ILogger _logger;

        public RpcTcpConnection(TcpPeer parent, RpcConfiguration configuration) : base(parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            this._configuration = configuration;

            RpcControllerContext context = new RpcControllerContext(
                configuration, this);

            _controller = new RpcController(context);
            _logger = configuration.LogManager.GetLogger(typeof(RpcTcpConnection));
        }

        /// <summary>
        /// Returns the memory used by this connection
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            _controller?.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnConnectionClosed(ConnectionClosedEventArgs args)
        {
            _controller?.Dispose();
            base.OnConnectionClosed(args);
        }

        protected override void OnMessageReceived(MessageEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            
            // _logger.Trace($"#{Id} received {args.Message}");
            try
            {
                _controller.OnMessage(args.Message);
            }
            catch (OperationCanceledException)
            {
                
            }
            catch (Exception e)
            {
                _logger.Error($"#{Id} got an unhandled exception in {nameof(RpcTcpConnection)}.{nameof(OnMessageReceived)}: {e}");
                Close();
            }
            finally
            {
                args?.Message?.Dispose();
            }
        }

        protected override void PollEvents()
        {
            base.PollEvents();
            _controller.PollEvents();
        }

        public RawMessage CreateMessage()
        {
            return Parent.CreateMessage();
        }
        
        public RawMessage CreateMessage(int length)
        {
            return Parent.CreateMessage(length);
        }

        public Task SendMessage(IRawMessage message, DeliveryType deliveryType, int channel, CancellationToken cancellationToken)
        {
            return base.SendMessageAsync(message, cancellationToken);
        }
    }
}
