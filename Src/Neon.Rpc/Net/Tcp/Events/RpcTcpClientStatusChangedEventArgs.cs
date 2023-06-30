using System;

namespace Neon.Rpc.Net.Tcp.Events
{
    public class RpcTcpClientStatusChangedEventArgs
    {
        /// <summary>
        /// A sender of the event
        /// </summary>
        public RpcTcpClient Sender { get; private set; }
        /// <summary>
        /// Previous status
        /// </summary>
        public  RpcClientStatus OldStatus { get; private set; }
        /// <summary>
        /// New status
        /// </summary>
        public  RpcClientStatus NewStatus { get; private set; }

        internal RpcTcpClientStatusChangedEventArgs(RpcTcpClient sender, RpcClientStatus oldStatus, RpcClientStatus newStatus)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));
            Sender = sender;
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }
}
