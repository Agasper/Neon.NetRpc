using System;
using Neon.Networking.Messages;

namespace Neon.Networking.Tcp.Events
{
    public class MessageEventArgs : IDisposable
    {
        public TcpConnection Connection { get; }
        public IRawMessage Message { get; }

        internal MessageEventArgs(TcpConnection connection, IRawMessage message)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            Connection = connection;
            Message = message;
        }

        public void Dispose()
        {
            Message?.Dispose();
        }
    }
}