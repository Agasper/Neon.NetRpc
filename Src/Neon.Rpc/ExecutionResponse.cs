using Neon.Rpc.Payload;

namespace Neon.Rpc
{
    public struct ExecutionResponse
    {
        /// <summary>
        /// Does response has result
        /// </summary>
        public bool HasResult { get; private set; }
        /// <summary>
        /// Nullable response
        /// </summary>
        public object Result { get; private set; }
        
        public ExecutionResponse(bool hasResult, object result)
        {
            this.HasResult = hasResult;
            this.Result = result;
        }
        
        internal ExecutionResponse(RemotingResponse response)
        {
            this.HasResult = response.HasArgument;
            this.Result = response.Argument;
        }

        public static ExecutionResponse FromResult(object result)
        {
            return new ExecutionResponse()
            {
                HasResult = true,
                Result = result
            };
        }

        public static ExecutionResponse NoResult
        {
            get
            {
                return new ExecutionResponse()
                {
                    HasResult = false
                };
            }
        }
    }
}