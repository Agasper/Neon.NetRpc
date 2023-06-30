namespace Neon.Networking.Udp.Events
{
    public class ConnectionClosedEventArgs
    {
        public UdpConnection Connection { get; }
        public DisconnectReason Reason { get; }

        public ConnectionClosedEventArgs(UdpConnection connection, DisconnectReason reason)
        {
            Connection = connection;
            Reason = reason;
        }
    }
}