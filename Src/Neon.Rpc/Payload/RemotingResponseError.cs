using Neon.Rpc.Serialization;

namespace Neon.Rpc.Payload
{
    public class RemotingResponseError : INeonMessage
    {
        public uint RequestId { get; set; }
        public object MethodKey { get; set; }
        public ulong ExecutionTime { get; set; }
        public RemotingException Exception { get; set; }

        public RemotingResponseError()
        {
        }

        public override string ToString()
        {
            return $"{nameof(RemotingResponseError)}[id={RequestId},method={MethodKey},exc={Exception.Message}]";
        }

        public RemotingResponseError(uint requestId, object methodKey, RemotingException remotingException)
        {
            this.RequestId = requestId;
            this.MethodKey = methodKey;
            this.Exception = remotingException;
        }

        public void MergeFrom(IRpcMessage message)
        {
            byte serviceByte = message.ReadByte();
            if (serviceByte == 1)
                MethodKey = message.ReadVarInt32();
            else
                MethodKey = message.ReadString();
            this.RequestId = message.ReadVarUInt32();
            this.ExecutionTime = message.ReadVarUInt64();
            this.Exception = new RemotingException(message);
        }

        public void WriteTo(IRpcMessage message)
        {
            message.Write((byte)RpcSessionBase.MESSAGE_TOKEN);
            message.Write((byte)MessageType.RpcResponseError);
            byte serviceByte = 0;
            if (MethodKey is int)
                serviceByte = 1;
            message.Write(serviceByte);
            if (MethodKey is int)
                message.WriteVarInt((int)MethodKey);
            else
                message.Write(MethodKey.ToString());
            message.WriteVarInt(RequestId);
            message.WriteVarInt(ExecutionTime);
            Exception.WriteTo(message);
        }
    }
}
