using Neon.Rpc.Authorization;

namespace Neon.Test.Util;

public class TestAuthSessionFactory : IAuthSessionFactory
{
    public bool ReturnAsync { get; set; }
    
    public AuthSessionServerBase CreateSession(AuthSessionContext sessionContext)
    {
        if (ReturnAsync)
            return new TestAuthSessionAsync(sessionContext);
        else
            return new TestAuthSession(sessionContext);
    }
}