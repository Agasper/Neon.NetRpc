using System;
using Google.Protobuf;
using Neon.Networking.Messages;
using Neon.Rpc.Messages.Proto;
using Neon.Util.Pooling;

namespace Neon.Rpc.Messages
{
    public abstract class RpcMessageBase : IDisposable
    {
        public int MessageId { get => _header.MessageId; set => _header.MessageId = value; }
        public bool HasPayload { get => _header.HasPayload; set => _header.HasPayload = value; }
        public int PayloadSize
        {
            get
            {
                if (_payload == null)
                    return 0;
                return _payload.Size;
            }
        }

        public IRpcPayload Payload => _payload;

        readonly IMemoryManager _memoryManager;

        private protected readonly RpcMessageHeaderProto _header;
        RpcPayload _payload;

        public RpcMessageBase(IMemoryManager memoryManager)
        {
            _header = new RpcMessageHeaderProto();
            _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            _payload = null;
        }
        
        internal RpcMessageBase(IMemoryManager memoryManager, RpcMessageHeaderProto header, RpcPayload payload)
        {
            _header = header ?? throw new ArgumentNullException(nameof(header));
            _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            _payload = payload;
        }
        
        internal static int GetHeaderSize(RpcMessageHeaderProto header)
        {
            int size = header.CalculateSize();
            return size + GetVarIntBytes(size);
        }
        
        static int GetVarIntBytes(int value)
        {
            if (value < 128)
                return 1;

            int cnt = 0;
            do
            {
                value >>= 7;
                cnt++;
            } while (value > 0);

            return cnt;
        }

        internal static RpcMessageHeaderProto ExtractHeader(IRawMessage message)
        {
            message.Position = 0;
            return message.ReadObjectDelimited<RpcMessageHeaderProto>();
        }
        
        internal static RpcPayload ExtractPayload(IMemoryManager memoryManager, IRawMessage message, RpcMessageHeaderProto header)
        {
            if (!header.HasPayload)
                return null;
            int payloadSize = message.Length - GetHeaderSize(header);
            RpcPayload payload = new RpcPayload(memoryManager.RentArray(payloadSize), payloadSize);
            if (payloadSize > 0)
            {
                message.Read(payload.Array, 0, payloadSize);
            }
            return payload;
        }

        public void GetPayloadTo(ref IMessage message)
        {
            if (_payload == null)
                throw new InvalidOperationException("Message has no payload");
            message.MergeFrom(_payload.Array.AsSpan(0, _payload.Size));
        }

        public IMessage GetPayload(MessageParser parser)
        {
            if (_payload == null)
                throw new InvalidOperationException("Message has no payload");
            return parser.ParseFrom(_payload.Array.AsSpan(0, _payload.Size));
        }
        
        public T GetPayload<T>() where T : IMessage, new()
        {
            if (_payload == null)
                throw new InvalidOperationException("Message has no payload");
            if (_payload.Size == 0)
                return new T();

            T result = new T();
            result.MergeFrom(_payload.Array.AsSpan(0, _payload.Size));
            return result;
        }

        public void SetPayload(IMessage payloadMessage)
        {
            if (payloadMessage == null) 
                throw new ArgumentNullException(nameof(payloadMessage));
            int payloadSize = payloadMessage.CalculateSize();
            _payload?.Dispose();
            _payload = new RpcPayload(_memoryManager.RentArray(payloadSize), payloadSize);
            _header.HasPayload = true;
            using(CodedOutputStream cos = new CodedOutputStream(_payload.Array))
                payloadMessage.WriteTo(cos);
        }

        public void ClearPayload()
        {
            _header.HasPayload = false;
            _payload.Dispose();
            _payload = new RpcPayload(_memoryManager.RentArray(0), 0);
        }
        
        public int CalculateSize()
        {
            int size = GetHeaderSize(_header);
            if (_payload != null)
                size += _payload.Size;
            return size;
        }

        public void WriteTo(IRawMessage rawMessage)
        {
            rawMessage.WriteObjectDelimited(_header);
            if (_payload != null && _payload.Size > 0)
                rawMessage.Write(_payload.Array, 0, _payload.Size);
        }
        
        public override string ToString()
        {
            return ToString(false);
        }
        
        public virtual string ToString(bool printHeader)
        {
            string result = string.Empty;
            if (_header != null)
            {
                if (_header.Data != null)
                    result =
                        $"{this.GetType().Name}[id={_header.MessageId},type={_header.Data.MessageTypeCase},payloadSize={PayloadSize}]";
                else
                    result =
                        $"{this.GetType().Name}[id={_header.MessageId},type=Unknown,payloadSize={PayloadSize}]";
            }

            if (printHeader && _header != null)
                result += " " + _header.ToString();
            return result;
        }

        public void Dispose()
        {
            _payload?.Dispose();
        }
    }
}