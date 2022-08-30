using System.Diagnostics;
using System.Threading.Tasks;
using Neon.Rpc;
using Neon.Rpc.Net.Tcp;
using Neon.Test.Proto;

namespace Neon.Test.Util;

public class TaskTest
{
    readonly RpcSession session;
    
    public TaskTest(RpcSession session)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public async Task Run()
    {
        Trace.Assert(await session.ExecuteAsync<int,int>("TestGenericTask", 100).ConfigureAwait(false) == 100);
        await session.ExecuteAsync("TestNonGenericTask").ConfigureAwait(false);
    }

}