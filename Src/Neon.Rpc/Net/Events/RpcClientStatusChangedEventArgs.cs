using System;
using Neon.Rpc.Net.Tcp;

namespace Neon.Rpc.Net.Events
{
    public class RpcClientStatusChangedEventArgs
    {
        public object Sender { get; private set; }
        public  RpcClientStatus OldStatus { get; private set; }
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
