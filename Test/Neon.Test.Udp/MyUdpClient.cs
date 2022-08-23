using Neon.Networking.Udp;
using Neon.Networking.Udp.Events;

namespace Neon.Test.Udp;

public class MyUdpClient : UdpClient
{
    public delegate void DOnConnectionOpened(ConnectionOpenedEventArgs args);
    public delegate void DOnConnectionClosed(ConnectionClosedEventArgs args);
    public delegate void DOnClientStatusChanged(ClientStatusChangedEventArgs args);
    
    public event DOnConnectionOpened? OnConnectionOpenedEvent;
    public event DOnConnectionClosed? OnConnectionClosedEvent;
    public event DOnClientStatusChanged? OnClientStatusChangedEvent;

    public MyUdpClient(UdpConfigurationClient configuration) : base(configuration)
    {
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

    protected override void OnClientStatusChanged(ClientStatusChangedEventArgs args)
    {
        OnClientStatusChangedEvent?.Invoke(args);
        base.OnClientStatusChanged(args);
    }

    protected override UdpConnection CreateConnection()
    {
        return new MyUdpConnection(this);
    }
}