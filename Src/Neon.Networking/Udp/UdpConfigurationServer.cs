using System;

namespace Neon.Networking.Udp
{
    public class UdpConfigurationServer : UdpConfigurationPeer
    {
        /// <summary>
        ///     Sets the server maximum connections. All the new connections after the limit will be dropped (default:
        ///     int.Maxvalue)
        /// </summary>
        public int MaximumConnections
        {
            get => _maximumConnections;
            set
            {
                CheckLocked();
                _maximumConnections = value;
            }
        }

        int _maximumConnections;

        public UdpConfigurationServer()
        {
            _maximumConnections = int.MaxValue;
        }

        internal override void Validate()
        {
            base.Validate();
            if (MaximumConnections < 0)
                throw new ArgumentException(
                    $"{nameof(MaximumConnections)} must be equal or greater than 0");
        }
    }
}