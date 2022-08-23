using Neon.Networking.Udp;
using Neon.Networking.Udp.Events;
using Neon.Networking.Udp.Messages;

namespace Neon.Test.Udp;

public class MyUdpConnection : UdpConnection
{
    public MyUdpConnection(UdpPeer peer) : base(peer)
    {

    }
}