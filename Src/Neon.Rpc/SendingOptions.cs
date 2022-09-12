using System;
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
        /// Should we ask server for method completion status to call OnRemoteExecutionException in case of exception
        /// </summary>
        [Obsolete]
        public bool ExpectResponse { get; set; }
        /// <summary>
        /// For UDP transport sets channel
        /// </summary>
        public int Channel { get; set; }
        /// <summary>
        /// For UDP transport sets delivery type
        /// </summary>
        public DeliveryType DeliveryType { get; set; }

        public SendingOptions(bool expectResponse, object state, int channel, DeliveryType deliveryType)
        {
            this.State = state;
#pragma warning disable CS0612
            this.ExpectResponse = expectResponse;
#pragma warning restore CS0612
            this.Channel = channel;
            this.DeliveryType = deliveryType;
        }

        [Obsolete()]
        public SendingOptions WithExpectResponse(bool expectResponse)
        {
            this.ExpectResponse = expectResponse;
            return this;
        }
        
        public SendingOptions WithDeliveryType(DeliveryType deliveryType)
        {
            this.DeliveryType = deliveryType;
            return this;
        }

        public SendingOptions WithChannel(int channel)
        {
            this.Channel = channel;
            return this;
        }

        public static SendingOptions Default => new SendingOptions(false, null, UdpConnection.DEFAULT_CHANNEL, DeliveryType.ReliableOrdered);

        public override string ToString()
        {
            return $"SendingOptions[state={State},expectResponse={ExpectResponse},channel={Channel},delivery_type={DeliveryType}]";
        }
    }
}
