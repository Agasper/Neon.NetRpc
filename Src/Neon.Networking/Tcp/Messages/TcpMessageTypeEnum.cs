namespace Neon.Networking.Tcp.Messages
{
    enum TcpMessageTypeEnum : byte
    {
        UserData = 0,
        KeepAliveRequest = 1,
        KeepAliveResponse = 2
    }
}