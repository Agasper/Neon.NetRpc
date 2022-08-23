using Neon.Networking.Udp;
using Neon.Networking.Udp.Events;
using Neon.Networking.Udp.Messages;

namespace Neon.Test.Udp;

public class MyUdpConnection : UdpConnection
{
    Timer timer;
    
    public MyUdpConnection(UdpPeer peer) : base(peer)
    {

    }

    protected override void OnConnectionOpened(ConnectionOpenedEventArgs args)
    {
        base.OnConnectionOpened(args);
    }

    void Tick(object? state)
    {
        this.CloseAsync();
    }

    public override void Dispose()
    {
        this.timer?.Dispose();
        base.Dispose();
    }

    protected override void OnMessageReceived(UdpRawMessage udpRawMessage)
    {
        base.OnMessageReceived(udpRawMessage);
    }
}