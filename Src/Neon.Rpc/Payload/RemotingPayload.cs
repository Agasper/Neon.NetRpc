using Neon.Rpc.Serialization;

namespace Neon.Rpc.Payload
{
    public class RemotingPayload : INeonMessage
    {
        public bool HasArgument { get; set; }
        public object Argument { get; set; }

        protected byte serviceByte;

        public RemotingPayload()
        {
        }

        public virtual void MergeFrom(IRpcMessage message)
        {
            this.serviceByte = message.ReadByte();
            HasArgument = (serviceByte & 1) == 1;
            if (HasArgument)
                Argument = message.ReadObject();
        }

        public virtual void WriteTo(IRpcMessage message)
        {
            if (HasArgument)
                serviceByte |= 1;

            message.Write(serviceByte);
            
            if (HasArgument)
                message.WriteObject(Argument);
        }
    }
}
