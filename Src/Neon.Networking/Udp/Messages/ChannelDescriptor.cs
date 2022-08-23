using System;
using System.Collections.Generic;

namespace Neon.Networking.Udp.Messages
{
    struct ChannelDescriptor : IEquatable<ChannelDescriptor>
    {
        public const int DEFAULT_CHANNEL = 0;
        
        public int Channel
        {
            get => channel;
            set
            {
                CheckChannelValue(value);
                channel = value;
            }
        }
        public DeliveryType DeliveryType => deliveryType;

        int channel;
        DeliveryType deliveryType;

        public ChannelDescriptor(int channel, DeliveryType deliveryType)
        {
            CheckChannelValue(channel);
            this.channel = channel;
            this.deliveryType = deliveryType;
        }

        public static void CheckChannelValue(int channel)
        {
            if (channel > MAX_CHANNEL || channel < 0)
                throw new ArgumentOutOfRangeException(nameof(channel), $"Channel should be in range 0-{MAX_CHANNEL}");
        }

        public const int MAX_CHANNEL = 7;


        public bool Equals(ChannelDescriptor other)
        {
            return channel == other.channel && deliveryType == other.deliveryType;
        }

        public override bool Equals(object obj)
        {
            return obj is ChannelDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (channel * 397) ^ (int) deliveryType;
            }
        }

        public static bool operator ==(ChannelDescriptor a, ChannelDescriptor b)
        {
            return a.Equals(b);
        }
        
        public static bool operator !=(ChannelDescriptor a, ChannelDescriptor b)
        {
            return !(a == b);
        }
    }
}
