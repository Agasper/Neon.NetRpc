using System;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking;
using Neon.Networking.Messages;
using Neon.Networking.Udp;
using Neon.Networking.Udp.Messages;
using Neon.Rpc.Controllers;
using ConnectionClosedEventArgs = Neon.Networking.Udp.Events.ConnectionClosedEventArgs;

namespace Neon.Rpc.Net.Udp
{
    public class RpcUdpConnection : UdpConnection, IRpcConnectionInternal
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

        internal RpcUdpConnection(UdpPeer parent, RpcConfiguration configuration) : base(parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            this._configuration = configuration;

            RpcControllerContext context = new RpcControllerContext(
                configuration, this);

            _controller = new RpcController(context);
            _logger = configuration.LogManager.GetLogger(typeof(RpcUdpConnection));
        }
        
        /// <summary>
        /// Returns the memory used by this connection
        /// </summary>
        public override void Dispose()
        {
            _controller?.Dispose();
            base.Dispose();
        }
        
        protected override void OnConnectionClosed(ConnectionClosedEventArgs args)
        {
            _controller?.Dispose();
            base.OnConnectionClosed(args);
        }

        protected override void OnMessageReceived(UdpMessageInfo udpMessageInfo)
        {
            // _logger.Trace($"#{Id} received {udpMessageInfo}");
            try
            {
                _controller.OnMessage(udpMessageInfo.Message);
            }
            catch (OperationCanceledException)
            {
                
            }
            catch (Exception e)
            {
                _logger.Error($"#{Id} got an unhandled exception in {nameof(RpcUdpConnection)}.{nameof(OnMessageReceived)}: {e}");
                _ = base.CloseAsync();
            }
            finally
            {
                udpMessageInfo.Dispose();
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
            return base.SendMessageAsync(new UdpMessageInfo(message, deliveryType, channel), cancellationToken);
        }

        public void Close()
        {
            _ = base.CloseAsync();
        }
    }
}
