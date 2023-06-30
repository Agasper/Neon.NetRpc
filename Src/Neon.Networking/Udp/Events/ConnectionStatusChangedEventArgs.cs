namespace Neon.Networking.Udp.Events
{
    public class ConnectionStatusChangedEventArgs
    {
        public UdpConnection Connection { get; }
        public UdpConnectionStatus Status { get; }

        internal ConnectionStatusChangedEventArgs(UdpConnectionStatus status, UdpConnection connection)
        {
            Connection = connection;
            Status = status;
        }
    }
}