using System;
using System.Threading;
using Neon.Networking.Messages;

namespace Neon.Networking.Tcp.Messages
{
    class TcpMessage : IDisposable
    {
        public MessageFlagsEnum Flags { get; set; }
        public MessageTypeEnum MessageType { get; set; }
        public RawMessage RawMessage { get; }
        public CancellationToken CancellationToken { get; }
        public bool DisposeAfterSend { get; }

        public TcpMessage(MessageFlagsEnum flags, MessageTypeEnum type, RawMessage rawMessage, CancellationToken cancellationToken, bool disposeAfterSend)
        {
            Flags = flags;
            MessageType = type;
            RawMessage = rawMessage;
            CancellationToken = cancellationToken;
            DisposeAfterSend = disposeAfterSend;
        }

        public void Dispose()
        {
            RawMessage?.Dispose();
        }

        public override string ToString()
        {
            return $"{nameof(TcpMessage)}[message={RawMessage},flags={Flags},type={MessageType}]";
        }
    }
}