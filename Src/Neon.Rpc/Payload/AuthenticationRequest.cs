using Neon.Rpc.Serialization;

namespace Neon.Rpc.Payload
{
    class AuthenticationRequest : RemotingPayload
    {
        public override void WriteTo(IRpcMessage message)
        {
            message.Write((byte)RpcSessionBase.MESSAGE_TOKEN);
            message.Write((byte)MessageType.AuthenticateRequest);
            base.WriteTo(message);
        }

        public override string ToString()
        {
            return $"{nameof(AuthenticationRequest)}[data_hidden]";
        }
    }
}