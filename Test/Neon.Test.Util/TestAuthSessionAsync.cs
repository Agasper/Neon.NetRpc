using Neon.Rpc.Authorization;
using Neon.Rpc.Payload;
using Neon.Test.Proto;

namespace Neon.Test.Util;

public class TestAuthSessionAsync : AuthSessionServerAsync
{
    public object? ResultServer { get; private set; }
    
    public TestAuthSessionAsync(AuthSessionContext sessionContext) : base(sessionContext)
    {

    }

    protected override async Task<object> Auth(object payload)
    {
        AuthTest authTest = payload as AuthTest ?? throw new InvalidOperationException();

        await Task.Delay(100);

        if (authTest.Login != authTest.Password)
            throw new RemotingException("Wrong credentials", RemotingException.StatusCodeEnum.AccessDenied);
    
        this.ResultServer = new AuthTestResult() {Result = "OK"};
        
        return ResultServer;
    }

    public struct AuthResult
    {
        public bool result;
    }
}