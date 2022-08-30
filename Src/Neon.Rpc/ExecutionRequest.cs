using Neon.Rpc.Payload;

namespace Neon.Rpc
{
    public class ExecutionRequest
    {
        /// <summary>
        /// Does request has the argument
        /// </summary>
        public bool HasArgument { get; private set; }
        /// <summary>
        /// Nullable argument
        /// </summary>
        public object Argument { get; private set; }
        /// <summary>
        /// Method identifier
        /// </summary>
        public object MethodKey { get; private set; }

        public ExecutionRequest(bool hasArgument, object argument, object methodKey)
        {
            this.HasArgument = hasArgument;
            this.Argument = argument;
            this.MethodKey = methodKey;
        }

        internal ExecutionRequest(RemotingRequest request) : this(request.HasArgument, request.Argument,
            request.MethodKey)
        {
            
        }
    }
}