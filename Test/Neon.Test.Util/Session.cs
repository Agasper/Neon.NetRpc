using Neon.Logging;
using Neon.Rpc;
using Neon.Rpc.Authorization;
using Neon.Rpc.Events;
using Neon.Test.Proto;

namespace Neon.Test.Util;

public class Session : RpcSessionImpl
{
    readonly SingleThreadSynchronizationContext context;
    
    public Session(RpcSessionContext sessionContext, SingleThreadSynchronizationContext context) : base(sessionContext)
    {
        if (sessionContext.AuthSession != null)
        {
            string? result = null;
            switch (sessionContext.AuthSession)
            {
                case TestAuthSession testAuthSession:
                    result = ((AuthTestResult)testAuthSession.ResultServer!)?.Result;
                    break;
                case TestAuthSessionAsync testAuthSessionAsync:
                    result = ((AuthTestResult)testAuthSessionAsync.ResultServer!)?.Result;
                    break;
                case AuthSessionClient authSessionClient:
                    result = ((AuthTestResult)authSessionClient.AuthResult).Result;
                    break;
                default:
                    throw new InvalidOperationException("Session auth test failed, auth result is undefined");
            }

            if (result != "OK")
                throw new InvalidOperationException($"Session auth check result failed, got {result}");
        }
        this.context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [RemotingMethod]
    TestMessage TestReturn(TestMessage message)
    {
        context.CheckThread();
        return message;
    }
    
    [RemotingMethod(1)]
    TestMessageWithId TestReturnId(TestMessageWithId message)
    {
        context.CheckThread();
        return message;
    }

    [RemotingMethod]
    BufferTestMessage BufferMessageTest(BufferTestMessage bufferTestMessage)
    {
        context.CheckThread();
        return bufferTestMessage;
    }

    protected override void OnLocalExecutionException(LocalExecutionExceptionEventArgs args)
    {
        logger.Critical($"Exception on method execution: {args.Request.MethodKey}");
        Aborter.Abort(127);
        base.OnLocalExecutionException(args);
    }
}