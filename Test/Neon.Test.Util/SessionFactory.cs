using Neon.Logging;
using Neon.Rpc;
using Neon.Rpc.Net;

namespace Neon.Test.Util;

public class SessionFactory : ISessionFactory
{
    readonly SingleThreadSynchronizationContext context;
    readonly bool testException;
    
    public SessionFactory(SingleThreadSynchronizationContext? context, bool testException = false)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.testException = testException;
    }
    
    public RpcSession CreateSession(RpcSessionContext sessionContext)
    {
        if (testException)
            throw new Exception("Test exception");
        return new Session(sessionContext, context);
    }
}