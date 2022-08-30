using System;
using Neon.Rpc.Serialization;

namespace Neon.Rpc.Payload
{
    class AuthenticationResponse : RemotingPayload
    {
        public RemotingException Exception { get; set; }

        public AuthenticationResponse()
        {
        }
        
        public override string ToString()
        {
            if (Exception != null)
                return $"{nameof(AuthenticationResponse)}[success=false, ex={Exception.Message}]";
            else
                return $"{nameof(AuthenticationResponse)}[success=true, has_arg={HasArgument}]";
        }

        public override void WriteTo(IRpcMessage message)
        {
            message.Write((byte) RpcSessionBase.MESSAGE_TOKEN);
            message.Write((byte) MessageType.AuthenticateResponse);
            base.WriteTo(message);

            byte serviceByte_ = 0;
            if (this.Exception != null)
                serviceByte_ |= 0b_0000_0001;
            
            message.Write(serviceByte_);
            if (this.Exception != null)
                Exception.WriteTo(message);
        }

        public override void MergeFrom(IRpcMessage message)
        {
            base.MergeFrom(message);
            byte serviceByte_ = message.ReadByte();
            if ((serviceByte_ & 0b_0000_0001) == 0b_0000_0001)
                this.Exception = new RemotingException(message);
        }
    }
}