using Neon.Rpc.Authorization;
using Neon.Rpc.Payload;
using Neon.Test.Proto;

namespace Neon.Test.Util;

public class TestAuthSession : AuthSessionServer
{
    public object ResultServer { get; private set; }
    
    public TestAuthSession(AuthSessionContext sessionContext) : base(sessionContext)
    {

    }

    // protected override Task<object> Auth(object payload)
    // {
    //     AuthTest authTest = payload as AuthTest ?? throw new InvalidOperationException();
    //     if (authTest.Login != authTest.Password)
    //         throw new RemotingException("Wrong credentials", RemotingException.StatusCodeEnum.AccessDenied);
    //
    //     this.ResultServer = new AuthTestResult() {Result = "OK"};
    //     
    //     return Task.FromResult<object>(ResultServer);
    // }

    protected override object Auth(object arg)
    {
        AuthTest authTest = arg as AuthTest ?? throw new InvalidOperationException();
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