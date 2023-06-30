using System;
using Neon.Rpc.Net.Tcp;

namespace Neon.Rpc.Net.Udp.Events
{
    public class RpcUdpClientStatusChangedEventArgs
    {
        /// <summary>
        /// A sender of the event
        /// </summary>
        public RpcUdpClient Sender { get; private set; }
        /// <summary>
        /// Previous status
        /// </summary>
        public  RpcClientStatus OldStatus { get; private set; }
        /// <summary>
        /// New status
        /// </summary>
        public  RpcClientStatus NewStatus { get; private set; }

        internal RpcUdpClientStatusChangedEventArgs(RpcUdpClient sender, RpcClientStatus oldStatus, RpcClientStatus newStatus)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));
            Sender = sender;
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }
}
