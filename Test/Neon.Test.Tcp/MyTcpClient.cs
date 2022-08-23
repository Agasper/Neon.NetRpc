using Neon.Networking.Tcp;
using Neon.Networking.Tcp.Events;

namespace Neon.Test.Tcp;

public class MyTcpClient : TcpClient
{
    public delegate void DOnConnectionOpened(ConnectionOpenedEventArgs args);
    public delegate void DOnConnectionClosed(ConnectionClosedEventArgs args);
    public delegate void DOnClientStatusChanged(ClientStatusChangedEventArgs args);
    
    public event DOnConnectionOpened? OnConnectionOpenedEvent;
    public event DOnConnectionClosed? OnConnectionClosedEvent;
    public event DOnClientStatusChanged? OnClientStatusChangedEvent;

    public MyTcpClient(TcpConfigurationClient configuration) : base(configuration)
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

    protected override TcpConnection CreateConnection()
    {
        return new MyTcpConnection(this);
    }
}