using Neon.Rpc;
using Neon.Rpc.Net;

namespace Neon.Test.Util;

public class SessionFactory : ISessionFactory
{
    readonly SingleThreadSynchronizationContext _context;
    readonly bool testException;
    
    public SessionFactory(SingleThreadSynchronizationContext? context, bool testException = false)
    {
        this._context = context ?? throw new ArgumentNullException(nameof(context));
        this.testException = testException;
    }

    public Task AuthenticateAsync(AuthenticationContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<RpcSessionBase> CreateSessionAsync(RpcSessionContext context, CancellationToken cancellationToken)
    {
        if (testException)
            throw new Exception("Test exception");
        return Task.FromResult<RpcSessionBase>(new UserSession(context, _context));
    }
}