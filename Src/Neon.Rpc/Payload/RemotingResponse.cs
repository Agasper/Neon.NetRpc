using Neon.Rpc.Serialization;

namespace Neon.Rpc.Payload
{
    class RemotingResponse : RemotingPayload
    {
        public uint RequestId { get; set; }
        public ulong ExecutionTime { get; set; }

        public RemotingResponse()
        {
            
        }

        public override string ToString()
        {
            string arg = "None";
            if (HasArgument)
                arg = Argument.GetType().Name;
            return $"{nameof(RemotingResponse)}[id={RequestId},arg={arg}]";
        }

        public override void MergeFrom(IRpcMessage message)
        {
            this.RequestId = message.ReadVarUInt32();
            this.ExecutionTime = message.ReadVarUInt64();
            base.MergeFrom(message);
        }

        public override void WriteTo(IRpcMessage message)
        {
            message.Write((byte)RpcSessionBase.MESSAGE_TOKEN);
            message.Write((byte)MessageType.RpcResponse);
            message.WriteVarInt(RequestId);
            message.WriteVarInt(ExecutionTime);
            base.WriteTo(message);
        }
    }
}
