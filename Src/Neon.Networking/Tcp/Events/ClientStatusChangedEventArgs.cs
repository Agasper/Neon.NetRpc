using static Neon.Networking.Tcp.TcpClient;

namespace Neon.Networking.Tcp.Events
{
    public class ClientStatusChangedEventArgs
    {
        public TcpClient Client { get; }
        public TcpClientStatus Status { get; }

        internal ClientStatusChangedEventArgs(TcpClientStatus newStatus, TcpClient client)
        {
            this.Client = client;
            this.Status = newStatus;
        }
    }
}
