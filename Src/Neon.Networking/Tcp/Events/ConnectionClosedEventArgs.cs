using System;
namespace Neon.Networking.Tcp.Events
{
    public class ConnectionClosedEventArgs
    {
        public TcpConnection Connection { get; }
        public Exception ClosingException { get; }

        internal ConnectionClosedEventArgs(TcpConnection connection, Exception ex)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            this.ClosingException = ex;
            this.Connection = connection;
        }
    }
}
