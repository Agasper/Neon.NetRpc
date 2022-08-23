using System;
namespace Neon.Rpc.Net.Events
{
    public class SessionOpenedEventArgs
    {
        public RpcSession Session { get;  }
        public IRpcConnection Connection { get; }

        internal SessionOpenedEventArgs(RpcSession session, IRpcConnection connection)
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
