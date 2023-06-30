using System;

namespace Neon.Networking.Tcp
{
    public class TcpConfigurationServer : TcpConfigurationPeer
    {
        /// <summary>
        ///     Sets the server maximum connections. All the new connections after the limit will be dropped (default:
        ///     int.MaxValue)
        /// </summary>
        public int MaximumConnections
        {
            get => _maximumConnections;
            set
            {
                CheckLocked();
                _maximumConnections = value;
            }
        }

        /// <summary>
        ///     The maximum length of the pending connections queue (default: 100)
        /// </summary>
        public int ListenBacklog
        {
            get => _listenBackLog;
            set
            {
                CheckLocked();
                _listenBackLog = value;
            }
        }

        /// <summary>
        ///     The amount of threads who accepting connections (default: 1)
        /// </summary>
        public int AcceptThreads
        {
            get => _acceptThreads;
            set
            {
                CheckLocked();
                _acceptThreads = value;
            }
        }

        protected int _acceptThreads;
        protected int _listenBackLog;
        protected int _maximumConnections;

        public TcpConfigurationServer()
        {
            _listenBackLog = 100;
            _acceptThreads = 1;
            _maximumConnections = int.MaxValue;
        }

        internal override void Validate()
        {
            base.Validate();
            if (AcceptThreads < 1 || AcceptThreads > 10)
                throw new ArgumentOutOfRangeException($"{nameof(AcceptThreads)} should be in range 1-10");
        }
    }
}