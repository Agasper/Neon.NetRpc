using System;
using System.Threading;
using Neon.Networking.Messages;

namespace Neon.Networking.Tcp.Messages
{
    class TcpMessage : IDisposable
    {
        public TcpMessageHeader Header { get; }
        public IRawMessage RawMessage { get; }
        public CancellationToken CancellationToken { get; }

        public TcpMessage(TcpMessageHeader header, IRawMessage rawMessage, CancellationToken cancellationToken)
        {
            Header = header;
            RawMessage = rawMessage;
            CancellationToken = cancellationToken;
        }

        public void Dispose()
        {
            RawMessage?.Dispose();
        }

        public override string ToString()
        {
            return $"{nameof(TcpMessage)}[message={RawMessage}]";
        }
    }
}