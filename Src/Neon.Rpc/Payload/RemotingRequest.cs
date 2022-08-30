using System;
using System.Threading;
using System.Threading.Tasks;
using Neon.Rpc.Serialization;
using Neon.Util;

namespace Neon.Rpc.Payload
{
    class RemotingRequest : RemotingPayload
    {
        public bool ExpectResult { get; set; }
        public bool ExpectResponse { get; set; }
        public object MethodKey { get; set; }
        public uint RequestId { get; set; }
        public DateTime Created { get; private set; }

        public TimeSpan RemoteExecutionTime
        {
            get
            {
                if (!executed.HasValue)
                    throw new InvalidOperationException($"{nameof(RemoteExecutionTime)} couldn't been calculated");
                return (executed.Value - Created);
            }
        }
        public RemotingResponse Response => response;

        DateTime? executed;

        public object Result
        {
            get
            {
                if (response == null)
                    throw new InvalidOperationException("Response is not received yet");
                if (!response.HasArgument)
                    throw new InvalidOperationException("Remote method return type is void or Task. Method executed but no result received");
                return response.Argument;
            }
        }

        RemotingResponse response;

        TaskCompletionSource<object> taskCompletionSource;

        public RemotingRequest()
        {
            Created = DateTime.UtcNow;
        }

        public override string ToString()
        {
            string arg = "None";
            if (HasArgument)
                arg = Argument.GetType().Name;
            return $"{nameof(RemotingRequest)}[id={RequestId},method={MethodKey},expectResponse={ExpectResult},arg={arg}]";
        }

        public override void MergeFrom(IRpcMessage message)
        {
            base.MergeFrom(message);
            this.RequestId = message.ReadVarUInt32();
            bool keyIsInt = (serviceByte & (1 << 1)) == (1 << 1);
            ExpectResult = (serviceByte & (1 << 2)) == (1 << 2);
            ExpectResponse = (serviceByte & (1 << 3)) == (1 << 3);
            if (keyIsInt)
                this.MethodKey = message.ReadVarInt32();
            else
                this.MethodKey = message.ReadString();
        }

        public override void WriteTo(IRpcMessage message)
        {
            message.Write((byte)RpcSessionBase.MESSAGE_TOKEN);
            message.Write((byte)MessageType.RpcRequest);
            if (MethodKey is int)
                serviceByte |= 1 << 1;
            if (ExpectResult)
                serviceByte |= 1 << 2;
            if (ExpectResponse)
                serviceByte |= 1 << 3;
            base.WriteTo(message);
            message.WriteVarInt(RequestId);
            if (MethodKey is int)
                message.WriteVarInt((int)MethodKey);
            else
                message.Write(MethodKey.ToString());
        }

        internal void CreateAwaiter()
        {
            taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        internal void SetCancelled()
        {
            if (taskCompletionSource == null)
                return;
            executed = DateTime.UtcNow;
            taskCompletionSource.TrySetCanceled();
        }

        internal void SetError(Exception exception)
        {
            if (taskCompletionSource == null)
                return;
            executed = DateTime.UtcNow;
            taskCompletionSource.TrySetException(exception);
        }

        internal void SetResult(RemotingResponse response)
        {
            if (taskCompletionSource == null)
                return;
            executed = DateTime.UtcNow;
            this.response = response;
            taskCompletionSource.TrySetResult(null);
        }

        internal async Task WaitAsync(int timeout, CancellationToken cancellationToken)
        {
            if (taskCompletionSource == null)
                throw new InvalidOperationException("Awaiter isn't created for this request");

            await taskCompletionSource.Task.TimeoutAfter(timeout, cancellationToken).ConfigureAwait(false);
        }
    }
}
