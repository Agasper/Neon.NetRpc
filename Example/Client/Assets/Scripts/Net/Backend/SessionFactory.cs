using Neon.Rpc;
using Neon.Rpc.Net;

namespace Neon.ClientExample.Net.Backend
{
    public class SessionFactory : ISessionFactory
    {
        public RpcSession CreateSession(RpcSessionContext sessionContext)
        {
            return new Session(sessionContext);
        }
    }
}