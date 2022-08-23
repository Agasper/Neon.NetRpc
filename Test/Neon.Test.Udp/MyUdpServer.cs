using Neon.Networking.Udp;
using Neon.Networking.Udp.Events;

namespace Neon.Test.Udp;

public class MyUdpServer : UdpServer
{
    public delegate void DOnConnectionOpened(ConnectionOpenedEventArgs args);
    public delegate void DOnConnectionClosed(ConnectionClosedEventArgs args);

    public event DOnConnectionOpened OnConnectionOpenedEvent;
    public event DOnConnectionClosed OnConnectionClosedEvent;
    
    public MyUdpServer(UdpConfigurationServer configuration) : base(configuration)
    {
    }

    protected override bool OnAcceptConnectionUnsynchronized(OnAcceptConnectionEventArgs args)
    {
        return true;
    }

    protected override void OnConnectionOpened(ConnectionOpenedEventArgs args)
    {
        OnConnectionOpenedEvent?.Invoke(args);
        base.OnConnectionOpened(args);
    }

    protected override void OnConnectionClosed(ConnectionClosedEventArgs args)
    {
        OnConnectionClosedEvent?.Invoke(args);
        base.OnConnectionClosed(args);
    }
    
    protected override UdpConnection CreateConnection()
    {
        return new MyUdpConnection(this);
    }
}