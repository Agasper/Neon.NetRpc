using System;
using Neon.Networking.Messages;

namespace Neon.Networking.Udp.Messages
{
    public class UdpRawMessage : IDisposable
    {
        public RawMessage Message { get; }
        public DeliveryType DeliveryType { get;  }
        public int Channel { get; }

        public UdpRawMessage(RawMessage message, DeliveryType deliveryType, int channel)
        {
            if (message == null) 
                throw new ArgumentNullException(nameof(message));
            ChannelDescriptor.CheckChannelValue(channel);
            this.Message = message;
            this.DeliveryType = deliveryType;
            this.Channel = channel;
        }

        public void Dispose()
        {
            Message?.Dispose();
        }

        public override string ToString()
        {
            return $"{nameof(UdpRawMessage)}[msg={Message},delivery={DeliveryType},channel={Channel}]";
        }
    }
}
