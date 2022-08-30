using System;
using Neon.Rpc.Net.Tcp;

namespace Neon.Rpc.Net.Events
{
    public class RpcClientStatusChangedEventArgs
    {
        /// <summary>
        /// A sender of the event
        /// </summary>
        public object Sender { get; private set; }
        /// <summary>
        /// Previous status
        /// </summary>
        public  RpcClientStatus OldStatus { get; private set; }
        /// <summary>
        /// New status
        /// </summary>
        public  RpcClientStatus NewStatus { get; private set; }

        internal RpcClientStatusChangedEventArgs(object sender, RpcClientStatus oldStatus, RpcClientStatus newStatus)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));
            this.Sender = sender;
            this.OldStatus = oldStatus;
            this.NewStatus = newStatus;
        }
    }
}
