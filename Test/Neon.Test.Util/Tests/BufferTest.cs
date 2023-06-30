using System.Diagnostics;
using Google.Protobuf;
using Neon.Rpc;
using Neon.Test.Proto;

namespace Neon.Test.Util;

public class BufferTest
{
    readonly RpcSessionBase _userSession;
    readonly int bufferSize;
    
    public BufferTest(RpcSessionBase userSession, int bufferSize)
    {
        this._userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        this.bufferSize = bufferSize;
    }

    byte[] GetRandomBytes(int count)
    {
        Random rnd = new Random();
        byte[] result = new byte[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = (byte)rnd.Next(0, 256);
        }

        return result;
    }

    public async Task Run()
    {
        BufferTestMessage msg = new BufferTestMessage();
        msg.Bytes = ByteString.CopyFrom(GetRandomBytes(bufferSize*2 + 20));

        var retMsg = await _userSession.ExecuteAsync<BufferTestMessage, BufferTestMessage>("BufferMessageTest", msg).ConfigureAwait(false);
        Trace.Assert(retMsg.Equals(msg));
    }
}