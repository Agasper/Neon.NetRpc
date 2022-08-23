using Neon.Networking.Tcp;
using Neon.Networking.Udp;
using Neon.Networking.Udp.Events;
using Neon.Networking.Udp.Messages;

namespace Neon.Test.Tcp;

public class MyTcpConnection : TcpConnection
{
    public MyTcpConnection(TcpPeer peer) : base(peer)
    {

    }
}