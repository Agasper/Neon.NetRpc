using System.Net;
using Neon.Networking.Messages;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp.Events
{
    public class OnAcceptConnectionEventArgs
    {
        public EndPoint Endpoint { get; private set; }

        public OnAcceptConnectionEventArgs(EndPoint endpoint)
        {
            this.Endpoint = endpoint;
        }
    }
}
