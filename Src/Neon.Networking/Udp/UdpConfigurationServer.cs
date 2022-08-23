﻿using System;

namespace Neon.Networking.Udp
{
    public class UdpConfigurationServer : UdpConfigurationPeer
    {
        public int MaximumConnections { get => maximumConnections; set { CheckLocked(); maximumConnections = value; } }

        int maximumConnections;

        internal override void Validate()
        {
            base.Validate();
            if (this.MaximumConnections < 0)
                throw new ArgumentException(
                    $"{nameof(this.MaximumConnections)} must be equal or greater than 0");
        }

        public UdpConfigurationServer()
        {
            maximumConnections = int.MaxValue;
            autoMtuExpand = false;
        }
    }
}
