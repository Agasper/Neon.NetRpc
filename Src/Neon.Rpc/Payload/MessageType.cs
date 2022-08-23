namespace Neon.Rpc.Payload
{
    enum MessageType : byte
    {
        RpcRequest = 1,
        RpcResponse = 2,
        RpcResponseError = 3,
        AuthenticateRequest = 4,
        AuthenticateResponse = 5
    }
}
