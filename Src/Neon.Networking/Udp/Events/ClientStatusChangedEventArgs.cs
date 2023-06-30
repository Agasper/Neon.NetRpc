namespace Neon.Networking.Udp.Events
{
    public class ClientStatusChangedEventArgs
    {
        public UdpClient Client { get; }
        public UdpClientStatus Status { get; }

        internal ClientStatusChangedEventArgs(UdpClientStatus status, UdpClient client)
        {
            Client = client;
            Status = status;
        }
    }
}