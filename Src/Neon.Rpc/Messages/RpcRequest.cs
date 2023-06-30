using System;
using Neon.Rpc.Messages.Proto;
using Neon.Util.Pooling;

namespace Neon.Rpc.Messages
{
    class RpcRequest : RpcMessageBase
    {
        public string Path { get => _header.Data.RpcRequest.Path; set => _header.Data.RpcRequest.Path = value; }
        public bool ExpectResponse { get => _header.Data.RpcRequest.ExpectResponse; set => _header.Data.RpcRequest.ExpectResponse = value; }

        internal RpcRequest(IMemoryManager memoryManager, RpcMessageHeaderProto header, RpcPayload payload) : base(memoryManager, header, payload)
        {
            if (header.Data.RpcRequest == null)
                throw new InvalidOperationException("Wrong header, expected RpcRequest");
            Path = header.Data.RpcRequest.Path;
            ExpectResponse = header.Data.RpcRequest.ExpectResponse;
        }

        public RpcRequest(IMemoryManager memoryManager) : base(memoryManager)
        {
            _header.Data = new RpcMessageHeaderDataProto();
            _header.Data.RpcRequest = new RpcMessageHeaderDataRpcRequestProto();
        }
        
        public override string ToString(bool printHeader)
        {
            string result = string.Empty;
            if (_header != null)
            {
                result =
                    $"{this.GetType().Name}[id={_header.MessageId},path={_header.Data.RpcRequest.Path},payloadSize={PayloadSize}]";
            }

            if (printHeader && _header != null)
                result += " " + _header.ToString();
            return result;
        }
    }
}