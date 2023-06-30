using Google.Protobuf.WellKnownTypes;
using Neon.Rpc;
using Neon.Rpc.Messages;
using Neon.Rpc.Net;
using Neon.Test.Proto;

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
        if (context.AuthenticationArgument != null && context.AuthenticationArgument.Is(AuthRequestMessage.Descriptor))
        {
            var authRequestMessage = context.AuthenticationArgument.Unpack<AuthRequestMessage>();
            if (authRequestMessage.Login == "test" &&
                authRequestMessage.Password == "test")
                return Task.CompletedTask;
        }

        throw new RpcException("Wrong credentials", RpcResponseStatusCode.Unauthenticated);
    }

    public Task<RpcSessionBase> CreateSessionAsync(RpcSessionContext context, CancellationToken cancellationToken)
    {
        if (testException || context.Connection.IsClientConnection)
            throw new Exception("Test exception");
        return Task.FromResult<RpcSessionBase>(new UserSession(context, _context));
    }
}