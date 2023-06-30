using System;
using Neon.Networking.Messages;

namespace Neon.Networking.Udp.Messages
{
    public class UdpMessageInfo : IDisposable
    {
        public IRawMessage Message { get; }
        public DeliveryType DeliveryType { get; }
        public int Channel { get; }

        public UdpMessageInfo(IRawMessage message, DeliveryType deliveryType, int channel)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            ChannelDescriptor.CheckChannelValue(channel);
            Message = message;
            DeliveryType = deliveryType;
            Channel = channel;
        }

        public void Dispose()
        {
            Message?.Dispose();
        }

        public override string ToString()
        {
            return $"{nameof(UdpMessageInfo)}[msg={Message},delivery={DeliveryType},channel={Channel}]";
        }
    }
}