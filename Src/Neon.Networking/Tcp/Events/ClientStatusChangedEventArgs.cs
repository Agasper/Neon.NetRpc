namespace Neon.Networking.Tcp.Events
{
    public class ClientStatusChangedEventArgs
    {
        public TcpClient Client { get; }
        public TcpClientStatus Status { get; }

        internal ClientStatusChangedEventArgs(TcpClientStatus newStatus, TcpClient client)
        {
            Client = client;
            Status = newStatus;
        }
    }
}