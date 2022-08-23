using Neon.Networking.Udp;
using Neon.Networking.Udp.Messages;

namespace Neon.Rpc
{
    public struct SendingOptions
    {
        public object State { get; set; }
        public bool ExpectResponse { get; set; }
        public int Channel { get; set; }
        public DeliveryType DeliveryType { get; set; }

        public SendingOptions(bool expectResponse, object state, int channel, DeliveryType deliveryType)
        {
            this.State = state;
            this.ExpectResponse = expectResponse;
            this.Channel = channel;
            this.DeliveryType = deliveryType;
        }

        public SendingOptions WithExpectResponse(bool expectResponse)
        {
            this.ExpectResponse = expectResponse;
            return this;
        }


        public static SendingOptions Default => new SendingOptions(false, null, UdpConnection.DEFAULT_CHANNEL, DeliveryType.ReliableOrdered);

        public override string ToString()
        {
            return $"SendingOptions[state={State},expectResponse={ExpectResponse},channel={Channel},delivery_type={DeliveryType}]";
        }
    }
}
