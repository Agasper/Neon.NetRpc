using System;
using Neon.Networking.Messages;

namespace Neon.Networking.Tcp.Events
{
    public class MessageEventArgs : IDisposable
    {
        public TcpConnection Connection { get;  }
        public RawMessage Message { get; }

        internal MessageEventArgs(TcpConnection connection, RawMessage message)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            this.Connection = connection;
            this.Message = message;
        }

        public void Dispose()
        {
            Message?.Dispose();
        }
    }
}
