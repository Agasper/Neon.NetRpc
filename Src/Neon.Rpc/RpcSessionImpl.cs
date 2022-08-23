using System;
using System.Threading.Tasks;

namespace Neon.Rpc
{
    public abstract class RpcSessionImpl : RpcSession
    {
        private protected readonly object remotingObject;
        private protected readonly RemotingObjectScheme remotingObjectScheme;
        
        protected RpcSessionImpl(RpcSessionContext sessionContext) : base(sessionContext)
        {
            this.remotingObject = this;
            this.remotingObjectScheme =
                new RemotingObjectScheme(sessionContext.RemotingInvocationRules, this.GetType());
        }
        
        protected override async Task<ExecutionResponse> ExecuteRequestAsync(ExecutionRequest request)
        {
            if (remotingObjectScheme == null || remotingObject == null)
                throw new NullReferenceException($"{nameof(RpcSession)} isn't initialized properly. Remoting object is null");

            var container = remotingObjectScheme.GetInvocationContainer(request.MethodKey);

            object result;
            if (request.HasArgument)
                result = await container.InvokeAsync(remotingObject, request.Argument).ConfigureAwait(false);
            else
                result = await container.InvokeAsync(remotingObject).ConfigureAwait(false);

            ExecutionResponse executionResponse = new ExecutionResponse(container.DoesReturnValue, result);

            return executionResponse;
        }
    }
}
