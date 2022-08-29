using System;
using System.Net;
using System.Threading;
using Neon.Logging;
using Neon.Networking;

namespace Neon.Rpc.Net.Tcp
{
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
        
        public RpcTcpClientReconnector(RpcTcpClient client, int delay, IPEndPoint endpoint, bool authRequired, object authObject)
        {
            this.delay = delay;
            this.client = client;
            this.endpoint = endpoint;
            this.authObject = authObject;
            this.authRequired = authRequired;
            this.useEndpoint = true;
        }
        
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

        public void Start()
        {
            Stop();
            timer = new Timer(Callback, null, INTERVAL, INTERVAL);
        }

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

        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}