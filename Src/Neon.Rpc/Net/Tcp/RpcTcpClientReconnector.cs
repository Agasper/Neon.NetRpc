using System;
using System.Net;
using System.Threading;
using Neon.Networking;

namespace Neon.Rpc.Net.Tcp
{
    /// <summary>
    /// Helper for maintaining client to server connection active
    /// </summary>
    public class RpcTcpClientReconnector : IDisposable
    {
        const int INTERVAL = 1000;
        Timer _timer;

        readonly RpcTcpClient _client;

        IPEndPoint _endpoint;
        string _host;
        int _port;
        int _delay;
        bool _useEndpoint;
        AuthenticationInfo _authenticationInfo;
        IPAddressSelectionRules _ipAddressSelectionRules;
        CancellationTokenSource _cts;

        DateTime? _disconnectedFrom;
        
        /// <summary>
        /// Helper for maintaining client to server connection active
        /// </summary>
        /// <param name="client">Instance of RpcTcpClient</param>
        /// <param name="delay">After connection dropped we should wait a delay before trying to establish a new connection</param>
        /// <param name="endpoint">Server endpoint</param>
        /// <param name="authenticationInfo">Authentication info</param>
        public RpcTcpClientReconnector(RpcTcpClient client, int delay, IPEndPoint endpoint, AuthenticationInfo authenticationInfo)
        {
            _delay = delay;
            _client = client;
            _endpoint = endpoint;
            _authenticationInfo = authenticationInfo;
            _useEndpoint = true;
        }
        
        /// <summary>
        /// Helper for maintaining client to server connection active
        /// </summary>
        /// <param name="client">Instance of RpcTcpClient</param>
        /// <param name="delay">After connection dropped we should wait a delay before trying to establish a new connection</param>
        /// <param name="host">Server IP address or domain</param>
        /// <param name="port">Server port</param>
        /// <param name="ipAddressSelectionRules">If destination host resolves to a few ap addresses, which we should take</param>
        /// <param name="authenticationInfo">Authentication info</param>
        public RpcTcpClientReconnector(RpcTcpClient client, int delay, string host, int port, IPAddressSelectionRules ipAddressSelectionRules, AuthenticationInfo authenticationInfo)
        {
            _ipAddressSelectionRules = ipAddressSelectionRules;
            _client = client;
            _delay = delay;
            _host = host;
            _port = port;
            _authenticationInfo = authenticationInfo;
            _useEndpoint = false;
        }

        /// <summary>
        /// Starting a reconnection timer
        /// </summary>
        public void Start()
        {
            Stop();
            _cts = new CancellationTokenSource();
            _timer = new Timer(Callback, _cts, INTERVAL, INTERVAL);
        }

        /// <summary>
        /// Stopping a reconnection timer
        /// </summary>
        public void Stop()
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
            _timer?.Dispose();
            _timer = null;
        }

        void Callback(object state)
        {
            if (_client.Status == RpcClientStatus.Disconnected && !_disconnectedFrom.HasValue)
            {
                _disconnectedFrom = DateTime.UtcNow;
                OnDisconnected();
            }
            
            if (_client.Status != RpcClientStatus.Disconnected)
            {
                _disconnectedFrom = null;
            }

            if (_disconnectedFrom.HasValue && _disconnectedFrom.Value.AddMilliseconds(_delay) < DateTime.UtcNow)
            {
                Go((CancellationToken)state);
                _disconnectedFrom = null;
            }
        }

        protected virtual void OnReconnect()
        {
            
        }
        
        protected virtual void OnDisconnected()
        {
            
        }

        async void Go(CancellationToken cancellationToken)
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
                
                if (_useEndpoint)
                    await _client.StartSessionAsync(_endpoint, _authenticationInfo, cancellationToken).ConfigureAwait(false);
                else
                    await _client.StartSessionAsync(_host, _port, _ipAddressSelectionRules, _authenticationInfo, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                _client.Close();
            }
        }

        /// <summary>
        /// Releases all resources used by the current instance of <see cref="T:Neon.Rpc.Net.Tcp.RpcTcpClientReconnector" />
        /// </summary>
        public void Dispose()
        {
            _cts?.Dispose();
            _timer?.Dispose();
        }
    }
}