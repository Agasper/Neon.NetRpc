namespace Neon.Networking.Tcp
{
    public class TcpConfigurationClient : TcpConfigurationPeer
    {
        /// <summary>
        /// Timeout for ConnectAsync method
        /// </summary>
        public int ConnectTimeout { get => connectTimeout; set { CheckLocked(); connectTimeout = value; } }
        
        protected int connectTimeout;
        
        public TcpConfigurationClient()
        {
            connectTimeout = 10000;
        }
    }
}
