namespace Neon.Rpc.Events
{
    public struct LocalExecutionStartingEventArgs
    {
        public ExecutionRequest Request { get; private set; }

        public LocalExecutionStartingEventArgs(ExecutionRequest remotingRequest)
        {
            this.Request = remotingRequest;
        }
    }
}