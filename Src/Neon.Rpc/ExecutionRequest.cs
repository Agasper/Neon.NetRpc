using Neon.Rpc.Payload;

namespace Neon.Rpc
{
    public class ExecutionRequest
    {
        public bool HasArgument { get; private set; }
        public object Argument { get; private set; }
        public object MethodKey { get; private set; }

        public ExecutionRequest(bool hasArgument, object argument, object methodKey)
        {
            this.HasArgument = hasArgument;
            this.Argument = argument;
            this.MethodKey = methodKey;
        }

        public ExecutionRequest(RemotingRequest request) : this(request.HasArgument, request.Argument,
            request.MethodKey)
        {
            
        }
    }
}