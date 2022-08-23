using System;
using Neon.Networking.Messages;

namespace Neon.Networking.Tcp.Messages
{
    class TcpMessage : IDisposable
    {
        public TcpMessageHeader Header { get; }
        public RawMessage RawMessage { get; }

        public TcpMessage(TcpMessageHeader header, RawMessage rawMessage)
        {
            this.Header = header;
            this.RawMessage = rawMessage;
        }

        public void Dispose()
        {
            this.RawMessage?.Dispose();
        }

        public override string ToString()
        {
            return $"{nameof(TcpMessage)}[size={Header.MessageSize}, type={Header.MessageType}, flags={Header.Flags}]";
        }
    }
}