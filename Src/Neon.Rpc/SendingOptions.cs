using System;
using System.Threading;
using Neon.Networking.Udp;
using Neon.Networking.Udp.Messages;

namespace Neon.Rpc
{
    public struct SendingOptions
    {
        /// <summary>
        /// User-defined state
        /// </summary>
        public object State { get; set; }
        /// <summary>
        /// For UDP transport sets channel
        /// </summary>
        public int Channel { get; set; }
        /// <summary>
        /// For UDP transport sets delivery type
        /// </summary>
        public DeliveryType DeliveryType { get; set; }
        /// <summary>
        /// Cancellation token for the operation
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        public SendingOptions(object state, int channel, DeliveryType deliveryType, CancellationToken cancellationToken)
        {
            State = state;
            Channel = channel;
            DeliveryType = deliveryType;
            CancellationToken = cancellationToken;
        }
        
        public SendingOptions WithState(object state)
        {
            State = state;
            return this;
        }

        public SendingOptions WithDeliveryType(DeliveryType deliveryType)
        {
            DeliveryType = deliveryType;
            return this;
        }

        public SendingOptions WithChannel(int channel)
        {
            Channel = channel;
            return this;
        }
        
        public SendingOptions WithCancellationToken(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            return this;
        }

        public static SendingOptions Default => new SendingOptions(null, UdpConnection.DEFAULT_CHANNEL, DeliveryType.ReliableOrdered, CancellationToken.None);

        public override string ToString()
        {
            return $"SendingOptions[state={State},channel={Channel},delivery_type={DeliveryType}]";
        }
    }
}
