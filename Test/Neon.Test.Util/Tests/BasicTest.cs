using System.Diagnostics;
using Neon.Rpc;
using Neon.Test.Proto;

namespace Neon.Test.Util;

public class BasicTest
{
    readonly RpcSessionBase _userSession;
    
    public BasicTest(RpcSessionBase userSession)
    {
        this._userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
    }

    public async Task Run()
    {
        var testMessage = GenerateMessage();
        var testMessageId = GenerateMessageId();
        Trace.Assert(testMessage.Equals(await _userSession.ExecuteAsync<TestMessage,TestMessage>("TestReturn", testMessage).ConfigureAwait(false)));
        Trace.Assert(testMessageId.Equals(await _userSession.ExecuteAsync<TestMessageWithId,TestMessageWithId>("NamedMethod", testMessageId).ConfigureAwait(false)));
        await _userSession.ExecuteAsync("TestGenericTask").ConfigureAwait(false);
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