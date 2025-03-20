using Neon.Networking.Messages;

namespace Neon.Networking.Tcp.Messages
{
    struct TcpMessageHeader
    {
        public int MessageSize { get; set; }
        public MessageFlagsEnum Flags { get; set; }
        public MessageTypeEnum MessageType { get; set; }

        public TcpMessageHeader(int size, MessageTypeEnum messageType, MessageFlagsEnum flags)
        {
            MessageSize = size;
            Flags = flags;
            MessageType = messageType;
        }

        public override string ToString()
        {
            return $"{nameof(TcpMessageHeader)}[size={MessageSize}, type={MessageType}, flags={Flags}]";
        }
    }
}