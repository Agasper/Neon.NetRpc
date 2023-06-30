using System;

namespace Neon.Networking.Udp
{
    public class UdpConfigurationClient : UdpConfigurationPeer
    {
        /// <summary>
        ///     Should we try to expand MTU after a connection established (default: true)
        /// </summary>
        public bool AutoMtuExpand
        {
            get => _autoMtuExpand;
            set
            {
                CheckLocked();
                _autoMtuExpand = value;
            }
        }

        /// <summary>
        ///     Max attempts to expand MTU in case of fail (default: 5)
        /// </summary>
        public int MtuExpandMaxFailAttempts
        {
            get => _mtuExpandMaxFailAttempts;
            set
            {
                CheckLocked();
                _mtuExpandMaxFailAttempts = value;
            }
        }

        /// <summary>
        ///     Interval for retrying MTU expand messages in case of MTU fail (default: 2000)
        /// </summary>
        public int MtuExpandFrequency
        {
            get => _mtuExpandFrequency;
            set
            {
                CheckLocked();
                _mtuExpandFrequency = value;
            }
        }

        bool _autoMtuExpand;
        int _mtuExpandFrequency;

        int _mtuExpandMaxFailAttempts;

        public UdpConfigurationClient()
        {
            _mtuExpandMaxFailAttempts = 5;
            _mtuExpandFrequency = 2000;
            _autoMtuExpand = true;
        }

        internal override void Validate()
        {
            base.Validate();
            if (MtuExpandFrequency < 1)
                throw new ArgumentException(
                    $"{nameof(MtuExpandFrequency)} must be greater than 0");
        }
    }
}