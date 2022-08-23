using System;
namespace Neon.Networking.Tcp.Events
{
    public class ConnectionOpenedEventArgs
    {
        public TcpConnection Connection { get; }

        internal ConnectionOpenedEventArgs(TcpConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            this.Connection = connection;
        }

    }
}
