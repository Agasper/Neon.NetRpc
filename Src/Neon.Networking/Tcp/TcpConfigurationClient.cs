using System.Threading;

namespace Neon.Networking.Tcp
{
    public class TcpConfigurationClient : TcpConfigurationPeer
    {
        /// <summary>
        /// Timeout for ConnectAsync method (default: -1/Infinite)
        /// </summary>
        public int ConnectTimeout { get => connectTimeout; set { CheckLocked(); connectTimeout = value; } }
        
        protected int connectTimeout;
        
        public TcpConfigurationClient()
        {
            connectTimeout = Timeout.Infinite;
        }
    }
}
