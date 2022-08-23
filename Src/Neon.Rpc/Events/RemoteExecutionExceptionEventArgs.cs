using Neon.Rpc.Payload;

namespace Neon.Rpc.Events
{
    public struct RemoteExecutionExceptionEventArgs
    {
        public ExecutionRequest Request { get; private set; }
        public RemotingException Exception { get; private set; }

        public RemoteExecutionExceptionEventArgs(ExecutionRequest remotingRequest, RemotingException exception)
        {
            this.Request = remotingRequest;
            this.Exception = exception;
        }
    }
}