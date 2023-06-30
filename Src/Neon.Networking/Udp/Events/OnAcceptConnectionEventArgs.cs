using System.Net;

namespace Neon.Networking.Udp.Events
{
    public class OnAcceptConnectionEventArgs
    {
        public EndPoint Endpoint { get; private set; }

        public OnAcceptConnectionEventArgs(EndPoint endpoint)
        {
            Endpoint = endpoint;
        }
    }
}