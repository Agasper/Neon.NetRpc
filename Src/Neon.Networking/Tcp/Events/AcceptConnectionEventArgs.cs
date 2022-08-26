using System.Net.Sockets;

namespace Neon.Networking.Tcp.Events
{
    public class AcceptConnectionEventArgs
    {
        public bool Accept { get; set; } = true;
        public Socket Socket { get; }

        internal AcceptConnectionEventArgs(Socket socket)
        {
            this.Socket = socket;
        }
    }
}