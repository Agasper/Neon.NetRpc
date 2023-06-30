using System;
using Neon.Rpc.Messages.Proto;
using Neon.Util.Pooling;

namespace Neon.Rpc.Messages
{
    public class RpcResponse : RpcMessageBase
    {
        public RpcResponseStatusCode StatusCode { get => (RpcResponseStatusCode)_header.Data.RpcResponse.StatusCode; set => _header.Data.RpcResponse.StatusCode = (int)value; }
        
        internal RpcResponse(IMemoryManager memoryManager, RpcMessageHeaderProto header, RpcPayload payload) : base(memoryManager, header, payload)
        {
            if (header.Data.RpcResponse == null)
                throw new InvalidOperationException("Wrong header, expected RpcResponse");
            StatusCode = (RpcResponseStatusCode)header.Data.RpcResponse.StatusCode;
        }
        
        public RpcResponse(IMemoryManager memoryManager) : base(memoryManager)
        {
            _header.Data = new RpcMessageHeaderDataProto();
            _header.Data.RpcResponse = new RpcMessageHeaderDataRpcResponseProto();
            StatusCode = RpcResponseStatusCode.Success;
        }
    }
}