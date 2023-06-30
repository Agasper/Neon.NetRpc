using System;

namespace Neon.Networking.Udp.Events
{
    public class ConnectionOpenedEventArgs
    {
        public UdpConnection Connection { get; }

        internal ConnectionOpenedEventArgs(UdpConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            Connection = connection;
        }
    }
}