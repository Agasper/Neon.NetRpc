using System;
using System.Net;

namespace Neon.Networking.Udp
{
    public struct UdpNetEndpoint : IEquatable<UdpNetEndpoint>
    {
        public EndPoint EndPoint { get; }

        string stringEp;

        public UdpNetEndpoint(EndPoint endPoint)
        {
            this.EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            this.stringEp = this.EndPoint.ToString();
        }

        public override string ToString()
        {
            return $"{nameof(UdpNetEndpoint)}[{this.stringEp}]";
        }

        public bool Equals(UdpNetEndpoint other)
        {
            return Equals(EndPoint, other.EndPoint);
        }

        public override bool Equals(object obj)
        {
            return obj is UdpNetEndpoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (EndPoint != null ? EndPoint.GetHashCode() : 0);
        }

        public static bool operator ==(UdpNetEndpoint a, UdpNetEndpoint b)
        {
            return a.Equals(b);
        }
        
        public static bool operator !=(UdpNetEndpoint a, UdpNetEndpoint b)
        {
            return !a.Equals(b);
        }
    }
}
