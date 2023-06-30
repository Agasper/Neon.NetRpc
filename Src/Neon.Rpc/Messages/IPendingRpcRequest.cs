namespace Neon.Rpc.Messages
{
    interface IPendingRpcRequest
    {
        void SetCancelled();
        void SetResult(RpcResponse response);
    }
}