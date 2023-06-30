using Neon.Rpc.Messages;

namespace Neon.Rpc.Controllers.States
{
    public struct UserState
    {
        public RpcSessionBase _session;
        public RpcObjectScheme _rpcObjectScheme;

        public void CheckInitialized()
        {
            if (_session == null || _rpcObjectScheme == null)
                throw new RpcException($"User session isn't initialized properly", RpcResponseStatusCode.FailedPrecondition);
        }
    }
}