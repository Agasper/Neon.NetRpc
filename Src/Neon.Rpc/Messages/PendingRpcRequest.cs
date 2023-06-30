using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Neon.Rpc.Messages.Proto;
using Neon.Util.Pooling;

namespace Neon.Rpc.Messages
{
    class PendingRpcRequest<T> : IPendingRpcRequest where T : IMessage<T>, new()
    {
        public bool ExpectResponse { get; set; }
        public IMessage Argument { get; set; }
        public int RequestId { get; set; }
        public string Method { get; set; }
        public DateTime Created { get; private set; }
        public RpcResponseStatusCode ResponseStatusCode { get; private set; }

        readonly TaskCompletionSource<T> taskCompletionSource;

        public PendingRpcRequest(bool createAwaiter)
        {
            Created = DateTime.UtcNow;
            if (createAwaiter)
                taskCompletionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        
        public RpcRequest CreateRpcRequest(IMemoryManager memoryManager)
        {
            RpcRequest request = new RpcRequest(memoryManager);
            request.Path = Method;
            request.MessageId = RequestId;
            request.ExpectResponse = ExpectResponse;
            request.HasPayload = Argument != null;
            if (Argument != null)
                request.SetPayload(Argument);
            return request;
        }

        void CheckAwaiter()
        {
            if (taskCompletionSource == null)
                throw new InvalidOperationException("Awaiter isn't created for this request");
        }

        public void SetCancelled()
        {
            CheckAwaiter();
            taskCompletionSource.TrySetCanceled();
        }
        
        public void SetResult(RpcResponse response)
        {
            CheckAwaiter();

            ResponseStatusCode = response.StatusCode;

            if (response.StatusCode != 0)
            {
                var protoException = response.GetPayload<RpcExceptionProto>();
                taskCompletionSource.TrySetException(new RpcException(protoException, response.StatusCode));
            }

            if (response.HasPayload)
                taskCompletionSource.TrySetResult(response.GetPayload<T>());
            else
                taskCompletionSource.TrySetResult(default);
        }

        public async Task<T> WaitAsync(CancellationToken cancellationToken)
        {
            CheckAwaiter();

            using(cancellationToken.Register(() => { SetCancelled(); }))
                return await taskCompletionSource.Task.ConfigureAwait(false);
        }
        
        public override string ToString()
        {
            return $"PendingRpcRequest[id={RequestId},method={Method}]";
        }

    }
    
}