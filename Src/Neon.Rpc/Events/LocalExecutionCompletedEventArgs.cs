namespace Neon.Rpc.Events
{
    public struct LocalExecutionCompletedEventArgs
    {
        public ExecutionRequest Request { get; private set; }
        public ExecutionResponse Response { get; private set; }
        public float ElapsedMilliseconds { get; private set; }
        
        public LocalExecutionCompletedEventArgs(ExecutionRequest request, ExecutionResponse response, float elapsedMilliseconds)
        {
            this.Request = request;
            this.Response = response;
            this.ElapsedMilliseconds = elapsedMilliseconds;
        }
    }
}