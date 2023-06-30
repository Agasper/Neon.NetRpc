using System.Threading;

namespace Neon.Networking.Tcp
{
    public class TcpConfigurationClient : TcpConfigurationPeer
    {
        /// <summary>
        ///     Timeout for ConnectAsync method (default: -1/Infinite)
        /// </summary>
        public int ConnectTimeout
        {
            get => _connectTimeout;
            set
            {
                CheckLocked();
                _connectTimeout = value;
            }
        }

        protected int _connectTimeout;

        public TcpConfigurationClient()
        {
            _connectTimeout = Timeout.Infinite;
        }
    }
}