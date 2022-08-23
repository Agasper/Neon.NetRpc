namespace Neon.Rpc.Events
{
    public struct RemoteExecutionCompletedEventArgs
    {
        public ExecutionRequest Request { get; private set; }
        public ExecutionResponse Response { get; private set; }
        public ExecutionOptions Options { get; private set; }
        public float ElapsedMilliseconds { get; private set; }
        
        public RemoteExecutionCompletedEventArgs(ExecutionRequest remotingRequest, ExecutionResponse response, ExecutionOptions options, float elapsedMilliseconds)
        {
            this.Request = remotingRequest;
            this.Response = response;
            this.Options = options;
            this.ElapsedMilliseconds = elapsedMilliseconds;
        }
    }
}