namespace Neon.Rpc.Net.Events
{
    public interface IRpcPeer
    {
        void OnSessionOpened(SessionOpenedEventArgs args);
        void OnSessionClosed(SessionClosedEventArgs args);
    }
}