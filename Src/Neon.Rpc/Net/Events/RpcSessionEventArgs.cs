namespace Neon.Rpc.Net.Events
{
    public class RpcSessionEventArgs
    {
        public RpcSessionBase Session { get; set; }

        public RpcSessionEventArgs(RpcSessionBase session)
        {
            Session = session;
        }
    }
}