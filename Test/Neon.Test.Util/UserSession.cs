using Google.Protobuf;
using Neon.Logging;
using Neon.Rpc;
using Neon.Test.Proto;

namespace Neon.Test.Util;

public class UserSession : RpcSessionBase
{
    readonly SingleThreadSynchronizationContext _context;
    readonly ILogger _logger;
    
    public UserSession(RpcSessionContext sessionContext, SingleThreadSynchronizationContext context) : base(sessionContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = sessionContext.LogManager.GetLogger(typeof(UserSession));
    }

    [RpcMethod]
    TestMessage TestReturn(TestMessage message)
    {
        _context.CheckThread();
        return message;
    }
    
    [RpcMethod("NamedMethod")]
    async Task<TestMessageWithId> TestAsyncReturnNamed(TestMessageWithId message)
    {
        await Task.Delay(100);
        _context.CheckThread();
        return message;
    }

    [RpcMethod()]
    async Task TestGenericTask()
    {
        await Task.Delay(100);
    }

    [RpcMethod]
    BufferTestMessage BufferMessageTest(BufferTestMessage bufferTestMessage)
    {
        _context.CheckThread();
        return bufferTestMessage;
    }

    
}