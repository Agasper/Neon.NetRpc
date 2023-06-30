using System;

namespace Neon.Networking.Udp.Messages
{
    struct ChannelDescriptor : IEquatable<ChannelDescriptor>
    {
        public const int DEFAULT_CHANNEL = 0;

        public int Channel
        {
            get => _channel;
            set
            {
                CheckChannelValue(value);
                _channel = value;
            }
        }

        public DeliveryType DeliveryType { get; }

        int _channel;

        public ChannelDescriptor(int channel, DeliveryType deliveryType)
        {
            CheckChannelValue(channel);
            _channel = channel;
            DeliveryType = deliveryType;
        }

        public static void CheckChannelValue(int channel)
        {
            if (channel > MAX_CHANNEL || channel < 0)
                throw new ArgumentOutOfRangeException(nameof(channel), $"Channel should be in range 0-{MAX_CHANNEL}");
        }

        public const int MAX_CHANNEL = 7;


        public bool Equals(ChannelDescriptor other)
        {
            return _channel == other._channel && DeliveryType == other.DeliveryType;
        }

        public override bool Equals(object obj)
        {
            return obj is ChannelDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_channel * 397) ^ (int) DeliveryType;
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