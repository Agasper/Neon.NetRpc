using Neon.Rpc.Payload;

namespace Neon.Rpc.Authorization
{
    /// <summary>
    /// Base auth session class
    /// </summary>
    public abstract class AuthSessionBase : RpcSessionBase
    {
        public AuthSessionBase(RpcSessionContextBase sessionContext) : base(sessionContext)
        {
        }
        
        private protected override void RemotingRequest(RemotingRequest remotingRequest)
        {
            RemotingResponseError remotingResponseError = new RemotingResponseError();
            remotingResponseError.Exception = new RemotingException("Authentication required",
                RemotingException.StatusCodeEnum.AccessDenied);
            remotingResponseError.ExecutionTime = 0;
            remotingResponseError.MethodKey = remotingRequest.MethodKey;
            remotingResponseError.RequestId = remotingRequest.RequestId;

            _ = SendNeonMessage(remotingResponseError);
        }

    }
}