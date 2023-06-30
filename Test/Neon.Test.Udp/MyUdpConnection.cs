using Neon.Networking.Udp;

namespace Neon.Test.Udp;

public class MyUdpConnection : UdpConnection
{
    public MyUdpConnection(UdpPeer peer) : base(peer)
    {

    }
}