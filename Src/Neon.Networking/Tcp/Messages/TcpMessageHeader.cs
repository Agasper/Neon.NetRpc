using Neon.Networking.Messages;

namespace Neon.Networking.Tcp.Messages
{
    struct TcpMessageHeader
    {
        public int MessageSize { get; set; }
        public TcpMessageFlagsEnum Flags { get; set; }
        public TcpMessageTypeEnum MessageType { get; set; }

        public TcpMessageHeader(int size, TcpMessageTypeEnum messageType, TcpMessageFlagsEnum flags)
        {
            this.MessageSize = size;
            this.Flags = flags;
            this.MessageType = messageType;
        }

        public static TcpMessageHeader FromMessage(RawMessage message, TcpMessageTypeEnum messageType,
            TcpMessageFlagsEnum additionalFlags = TcpMessageFlagsEnum.None)
        {
            TcpMessageFlagsEnum flags = additionalFlags;
            int size = 0;
            if (message != null)
            {
                size = message.Length;
                if (message.Compressed)
                    flags |= TcpMessageFlagsEnum.Compressed;
                if (message.Encrypted)
                    flags |= TcpMessageFlagsEnum.Encrypted;
            }

            return new TcpMessageHeader(size, messageType, flags);
        }
        
        public override string ToString()
        {
            return $"{nameof(TcpMessageHeader)}[size={MessageSize}, type={MessageType}, flags={Flags}]";
        }
    }
}