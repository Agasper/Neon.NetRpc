using System;
using System.Net;
using System.Threading;
using Neon.Logging;
using Neon.Networking;

namespace Neon.Rpc.Net.Tcp
{
    /// <summary>
    /// Helper for maintaining client to server connection active
    /// </summary>
    public class RpcTcpClientReconnector : IDisposable
    {
        const int INTERVAL = 1000;
        Timer timer;

        readonly RpcTcpClient client;

        IPEndPoint endpoint;
        string host;
        int port;
        int delay;
        bool useEndpoint;
        bool authRequired;
        object authObject;
        IPAddressSelectionRules ipAddressSelectionRules;

        DateTime? disconnectedFrom;
        
        /// <summary>
        /// Helper for maintaining client to server connection active
        /// </summary>
        /// <param name="client">Instance of RpcTcpClient</param>
        /// <param name="delay">After connection dropped we should wait a delay before trying to establish a new connection</param>
        /// <param name="endpoint">Server endpoint</param>
        /// <param name="authRequired">Does server require authentication</param>
        /// <param name="authObject">If server require authentication, the object we want to pass to</param>
        public RpcTcpClientReconnector(RpcTcpClient client, int delay, IPEndPoint endpoint, bool authRequired, object authObject)
        {
            this.delay = delay;
            this.client = client;
            this.endpoint = endpoint;
            this.authObject = authObject;
            this.authRequired = authRequired;
            this.useEndpoint = true;
        }
        
        /// <summary>
        /// Helper for maintaining client to server connection active
        /// </summary>
        /// <param name="client">Instance of RpcTcpClient</param>
        /// <param name="delay">After connection dropped we should wait a delay before trying to establish a new connection</param>
        /// <param name="host">Server IP address or domain</param>
        /// <param name="port">Server port</param>
        /// <param name="ipAddressSelectionRules">If destination host resolves to a few ap addresses, which we should take</param>
        /// <param name="authRequired">Does server require authentication</param>
        /// <param name="authObject">If server require authentication, the object we want to pass to</param>
        public RpcTcpClientReconnector(RpcTcpClient client, int delay, string host, int port, IPAddressSelectionRules ipAddressSelectionRules, bool authRequired, object authObject)
        {
            this.ipAddressSelectionRules = ipAddressSelectionRules;
            this.delay = delay;
            this.client = client;
            this.host = host;
            this.port = port;
            this.authObject = authObject;
            this.authRequired = authRequired;
            this.useEndpoint = false;
        }

        /// <summary>
        /// Starting a reconnection timer
        /// </summary>
        public void Start()
        {
            Stop();
            timer = new Timer(Callback, null, INTERVAL, INTERVAL);
        }

        /// <summary>
        /// Stopping a reconnection timer
        /// </summary>
        public void Stop()
        {
            timer?.Dispose();
            timer = null;
        }

        void Callback(object state)
        {
            if (client.Status == RpcClientStatus.Disconnected && !disconnectedFrom.HasValue)
            {
                disconnectedFrom = DateTime.UtcNow;
                OnDisconnected();
            }
            
            if (client.Status != RpcClientStatus.Disconnected)
            {
                disconnectedFrom = null;
            }

            if (disconnectedFrom.HasValue && disconnectedFrom.Value.AddMilliseconds(delay) < DateTime.UtcNow)
            {
                Go();
                disconnectedFrom = null;
            }
        }

        protected virtual void OnReconnect()
        {
            
        }
        
        protected virtual void OnDisconnected()
        {
            
        }

        async void Go()
        {
            try
            {
                // for (int i = 0; i < 3; i++)
                // {
                //     Console.Beep();
                //     Thread.Sleep(200);
                // }
                //
                OnReconnect();
                
                if (useEndpoint)
                    await client.OpenConnectionAsync(endpoint).ConfigureAwait(false);
                else
                    await client.OpenConnectionAsync(host, port, ipAddressSelectionRules).ConfigureAwait(false);
                if (authRequired)
                    await client.StartSessionWithAuth(authObject);
                else
                    await client.StartSessionNoAuth();
            }
            catch
            {
                client.Close();
            }
        }

        /// <summary>
        /// Releases all resources used by the current instance of <see cref="T:Neon.Rpc.Net.Tcp.RpcTcpClientReconnector" />
        /// </summary>
        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}