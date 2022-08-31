using System;

namespace Neon.Networking.Udp
{
    public class UdpConfigurationClient : UdpConfigurationPeer
    {
        /// <summary>
        /// Should we try to expand MTU after a connection established (default: true)
        /// </summary>
        public bool AutoMtuExpand { get => autoMtuExpand; set { CheckLocked(); autoMtuExpand = value; } }
        /// <summary>
        /// Max attempts to expand MTU in case of fail (default: 5)
        /// </summary>
        public int MtuExpandMaxFailAttempts { get => mtuExpandMaxFailAttempts; set { CheckLocked(); mtuExpandMaxFailAttempts = value; } }
        /// <summary>
        /// Interval for retrying MTU expand messages in case of MTU fail (default: 2000)
        /// </summary>
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
