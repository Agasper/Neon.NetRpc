using System;
using System.Net;

namespace Neon.Networking.Udp
{
    public struct UdpNetEndpoint : IEquatable<UdpNetEndpoint>
    {
        /// <summary>
        ///     The remote endpoint
        /// </summary>
        public EndPoint _EndPoint { get; }

        readonly string _stringEp;

        public UdpNetEndpoint(EndPoint endPoint)
        {
            _EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            _stringEp = _EndPoint.ToString();
        }

        public override string ToString()
        {
            return $"{nameof(UdpNetEndpoint)}[{_stringEp}]";
        }

        public bool Equals(UdpNetEndpoint other)
        {
            return Equals(_EndPoint, other._EndPoint);
        }

        public override bool Equals(object obj)
        {
            return obj is UdpNetEndpoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _EndPoint != null ? _EndPoint.GetHashCode() : 0;
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