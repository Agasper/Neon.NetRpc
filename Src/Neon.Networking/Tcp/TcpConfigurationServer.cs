using System;

namespace Neon.Networking.Tcp
{
    public class TcpConfigurationServer : TcpConfigurationPeer
    {
        /// <summary>
        /// Sets the server maximum connections. All the new connections after the limit will be dropped (default: int.MaxValue)
        /// </summary>
        public int MaximumConnections { get => maximumConnections; set { CheckLocked(); maximumConnections = value; } }
        /// <summary>
        /// The maximum length of the pending connections queue (default: 100)
        /// </summary>
        public int ListenBacklog { get => listenBackLog; set { CheckLocked(); listenBackLog = value; } }
        /// <summary>
        /// The amount of threads who accepting connections (default: 1)
        /// </summary>
        public int AcceptThreads { get => acceptThreads; set { CheckLocked(); acceptThreads = value; } }

        protected int acceptThreads;
        protected int listenBackLog;
        protected int maximumConnections;

        internal override void Validate()
        {
            base.Validate();
            if (this.AcceptThreads < 1 || this.AcceptThreads > 10)
                throw new ArgumentOutOfRangeException($"{nameof(this.AcceptThreads)} should be in range 1-10");
        }

        public TcpConfigurationServer()
        {
            listenBackLog = 100;
            acceptThreads = 1;
            maximumConnections = int.MaxValue;
        }
    }
}
