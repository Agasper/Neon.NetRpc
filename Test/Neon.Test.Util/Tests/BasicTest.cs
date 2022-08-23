using System.Diagnostics;
using System.Threading.Tasks;
using Neon.Rpc;
using Neon.Rpc.Net.Tcp;
using Neon.Test.Proto;

namespace Neon.Test.Util;

public class BasicTest
{
    readonly RpcSession session;
    
    public BasicTest(RpcSession session)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public async Task Run()
    {
        var testMessage = GenerateMessage();
        var testMessageId = GenerateMessageId();
        Trace.Assert(testMessage.Equals(await session.ExecuteAsync<TestMessage,TestMessage>("TestReturn", testMessage).ConfigureAwait(false)));
        Trace.Assert(testMessageId.Equals(await session.ExecuteAsync<TestMessageWithId,TestMessageWithId>(1, testMessageId).ConfigureAwait(false)));
    }
    
    TestMessage GenerateMessage()
    {
        TestMessage test = new TestMessage();
        test.Double = 12658934.53645646;
        test.Float = 2341.45123453f;
        test.Int = 234124324;
        test.Long = 5943765923487568;
        test.String = "TEST STRING";
        return test;
    }
        
    TestMessageWithId GenerateMessageId()
    {
        TestMessageWithId test = new TestMessageWithId();
        test.Double = 12658934.53645646;
        test.Float = 2341.45123453f;
        test.Int = 234124324;
        test.Long = 5943765923487568;
        test.String = "TEST STRING";
        return test;
    }
}