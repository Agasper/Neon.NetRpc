using System;
namespace Neon.Rpc.Net.Events
{
    public class SessionClosedEventArgs
    {
        public RpcSession Session { get; }
        public IRpcConnection Connection { get; }

        internal SessionClosedEventArgs(RpcSession session, IRpcConnection connection)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            this.Session = session;
            this.Connection = connection;
        }
    }
}
