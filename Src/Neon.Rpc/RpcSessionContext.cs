using System.Threading.Tasks;
using Neon.Logging;
using Neon.Rpc.Authorization;

namespace Neon.Rpc
{
    public class RpcSessionContext : RpcSessionContextBase
    {
        public int DefaultExecutionTimeout { get;  }
        public bool OrderedExecution { get;  }
        public int OrderedExecutionMaxQueue { get;  }
        public TaskScheduler TaskScheduler { get; }
        public AuthSessionBase AuthSession { get; }
        public RemotingInvocationRules RemotingInvocationRules { get; }

        internal RpcSessionContext(int defaultExecutionTimeout, bool orderedExecution, int orderedExecutionMaxQueue,
            TaskScheduler taskScheduler, ILogManager logManager, IRpcConnectionInternal rpcConnectionInternal,
            RemotingInvocationRules remotingInvocationRules, AuthSessionBase authSession)
            : base(logManager, rpcConnectionInternal)
        {
            this.DefaultExecutionTimeout = defaultExecutionTimeout;
            this.OrderedExecution = orderedExecution;
            this.OrderedExecutionMaxQueue = orderedExecutionMaxQueue;
            this.TaskScheduler = taskScheduler;
            this.RemotingInvocationRules = remotingInvocationRules;
            this.AuthSession = authSession;
        }
    }
}
