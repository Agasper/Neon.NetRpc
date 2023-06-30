using System;
using Google.Protobuf.WellKnownTypes;
using Neon.Rpc.Messages;

namespace Neon.Rpc.Controllers.States
{
    public struct AuthState
    {
        public DateTimeOffset _started;
        public bool _isAuthenticated;
        public object _authState;
        public Any _authResult;
        
        public void CheckCompleted()
        {
            if (!_isAuthenticated)
                throw new RpcException("Auth isn't completed", RpcResponseStatusCode.FailedPrecondition);
        }

        public void CheckNotCompleted()
        {
            if (_isAuthenticated)
                throw new RpcException("Auth already completed", RpcResponseStatusCode.FailedPrecondition);
        }

        public void CheckCanSend(bool isClientConnection)
        {
            CheckNotCompleted();   
            if (!isClientConnection)
                throw new RpcException("Sending auth request not supported on server connections", RpcResponseStatusCode.NotSupported);
        }

        public void CheckCanReceive(bool isClientConnection)
        {
            if (isClientConnection)
                throw new RpcException("Receiving auth request not supported on client connections", RpcResponseStatusCode.NotSupported);
            CheckNotCompleted();
        }
    }
}