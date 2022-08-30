using System;
namespace Neon.Rpc.Net.Events
{
    public class SessionOpenedEventArgs
    {
        /// <summary>
        /// Instance of the session
        /// </summary>
        public RpcSession Session { get;  }
        /// <summary>
        /// Instance of the connection bound to the session
        /// </summary>
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
