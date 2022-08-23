namespace Neon.Rpc.Net.Events
{
    public delegate void DOnSessionOpened(SessionOpenedEventArgs args);
    public delegate void DOnSessionClosed(SessionClosedEventArgs args);
    public delegate void DOnClientStatusChanged(RpcClientStatusChangedEventArgs args);
}