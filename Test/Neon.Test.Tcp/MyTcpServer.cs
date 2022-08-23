
using System.Net.Sockets;
using Neon.Networking.Tcp;
using Neon.Networking.Tcp.Events;

namespace Neon.Test.Tcp;

public class MyTcpServer : TcpServer
{
    public delegate void DOnConnectionOpened(ConnectionOpenedEventArgs args);
    public delegate void DOnConnectionClosed(ConnectionClosedEventArgs args);

    public event DOnConnectionOpened OnConnectionOpenedEvent;
    public event DOnConnectionClosed OnConnectionClosedEvent;
    
    public MyTcpServer(TcpConfigurationServer configuration) : base(configuration)
    {
    }

    protected override bool OnAcceptConnectionUnsynchronized(Socket socket)
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
    
    protected override TcpConnection CreateConnection()
    {
        return new MyTcpConnection(this);
    }
}