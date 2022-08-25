using System;

namespace Neon.Networking.Udp
{
    public class UdpConfigurationClient : UdpConfigurationPeer
    {
        public bool AutoMtuExpand { get => autoMtuExpand; set { CheckLocked(); autoMtuExpand = value; } }
        public int MtuExpandMaxFailAttempts { get => mtuExpandMaxFailAttempts; set { CheckLocked(); mtuExpandMaxFailAttempts = value; } }
        public int MtuExpandFrequency { get => mtuExpandFrequency; set { CheckLocked(); mtuExpandFrequency = value; } }

        int mtuExpandMaxFailAttempts;
        int mtuExpandFrequency;
        bool autoMtuExpand;

        internal override void Validate()
        {
            base.Validate();
            if (this.MtuExpandFrequency < 1)
                throw new ArgumentException(
                    $"{nameof(this.MtuExpandFrequency)} must be greater than 0");
        }

        public UdpConfigurationClient()
        {
            mtuExpandMaxFailAttempts = 5;
            mtuExpandFrequency = 2000;
            autoMtuExpand = true;
        }
    }
}
