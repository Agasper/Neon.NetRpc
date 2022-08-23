namespace Neon.Networking.Tcp
{
    public class TcpConfigurationClient : TcpConfigurationPeer
    {
        public int ConnectTimeout { get => connectTimeout; set { CheckLocked(); connectTimeout = value; } }
        
        protected int connectTimeout;
        
        public TcpConfigurationClient()
        {
            connectTimeout = 10000;
        }
    }
}
