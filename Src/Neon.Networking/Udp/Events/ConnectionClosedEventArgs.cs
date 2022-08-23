using Neon.Networking.Messages;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp.Events
{
    public class ConnectionClosedEventArgs
    {
        public UdpConnection Connection { get;  }
        public DisconnectReason Reason { get;  }

        public ConnectionClosedEventArgs(UdpConnection connection, DisconnectReason reason)
        {
            this.Connection = connection;
            this.Reason = reason;
        }
    }
}
