using Neon.Networking.Cryptography;
using Neon.Networking.Messages;
using Neon.Rpc.Cryptography.Ciphers;
using Neon.Rpc.Messages;

namespace Neon.Rpc.Controllers.States
{
    struct HandshakeState
    {
        public bool _handshakeCompleted;
        public IRpcCipher NegotiatedRpcCipher;

        public void CheckCompleted(IRpcCipher rpcCipher)
        {
            if (!_handshakeCompleted)
                throw new RpcException("Handshake isn't completed", RpcResponseStatusCode.FailedPrecondition);
            if (NegotiatedRpcCipher != rpcCipher)
                throw new RpcException("Encryption not set", RpcResponseStatusCode.FailedPrecondition);
        }

        public void CheckNotCompleted()
        {
            if (_handshakeCompleted)
                throw new RpcException("Handshake already completed", RpcResponseStatusCode.FailedPrecondition);
        }

        public void CheckCanSend(bool isClientConnection)
        {
            CheckNotCompleted();   
            if (!isClientConnection)
                throw new RpcException("Sending handshake request not supported on server connections", RpcResponseStatusCode.NotSupported);
        }

        public void CheckCanReceive(bool isClientConnection)
        {
            if (isClientConnection)
                throw new RpcException("Receiving handshake request not supported on client connections", RpcResponseStatusCode.NotSupported);
            CheckNotCompleted();
        }

        public IRpcCipher GetCipher()
        {
            if (NegotiatedRpcCipher == null)
                throw new RpcException("Cipher not set", RpcResponseStatusCode.FailedPrecondition);
            return NegotiatedRpcCipher;
        }
    }
}