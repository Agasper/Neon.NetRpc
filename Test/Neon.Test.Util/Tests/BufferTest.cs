using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Google.Protobuf;
using Neon.Rpc;
using Neon.Test.Proto;

namespace Neon.Test.Util;

public class BufferTest
{
    readonly RpcSession session;
    readonly int bufferSize;
    
    public BufferTest(RpcSession session, int bufferSize)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
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

        var retMsg = await session.ExecuteAsync<BufferTestMessage, BufferTestMessage>("BufferMessageTest", msg).ConfigureAwait(false);
        Trace.Assert(retMsg.Equals(msg));
    }
}