using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking.Udp;
using Neon.Networking.Udp.Messages;
using Neon.Rpc.Events;
using Neon.Rpc.Payload;
using Neon.Rpc.Serialization;
using Neon.Util;
using MessageType = Neon.Rpc.Payload.MessageType;

namespace Neon.Rpc
{
    public abstract class RpcSessionBase
    {
        internal const int MESSAGE_TOKEN = 241;

        public long Id => connection.Id;
        public object Tag { get; set; }
        public IRpcConnection Connection => connection;
        
        private protected readonly IRpcConnectionInternal connection;
        protected readonly ILogger logger;

        public RpcSessionBase(RpcSessionContextBase sessionContext)
        {
            if (sessionContext.Connection == null)
                throw new ArgumentNullException(nameof(sessionContext.Connection));
            if (sessionContext.LogManager == null)
                throw new ArgumentNullException(nameof(sessionContext.LogManager));
            
            this.connection = sessionContext.ConnectionInternal;
            this.logger = sessionContext.LogManager.GetLogger(nameof(RpcSessionBase));
            this.logger.Meta["kind"] = this.GetType().Name;
            this.logger.Meta["connection_id"] = this.connection.Id;
            this.logger.Meta["connection_endpoint"] = new RefLogLabel<IRpcConnection>(this.connection, s => s.RemoteEndpoint);
            this.logger.Meta["tag"] = new RefLogLabel<RpcSessionBase>(this, s => s.Tag);
            this.logger.Meta["latency"] = new RefLogLabel<RpcSessionBase>(this, s =>
            {
                var lat = s.connection.Statistics.Latency;
                if (lat.HasValue)
                    return lat.Value;
                else
                    return "";
            });
            
            this.logger.Debug($"{LogsSign} {this} created!");
        }

        void CheckConnected()
        {
            if (!connection.Connected)
                throw new InvalidOperationException($"{nameof(RpcSession)} is not established");
        }

        internal void OnMessage(IRpcMessage rpcMessage)
        {
            try
            {
                if (!connection.Connected)
                {
                    logger.Debug($"{LogsSign} got message when connection closed. Ignoring...");
                    return;
                }

                byte token = rpcMessage.ReadByte();
                if (token != MESSAGE_TOKEN)
                    throw new ArgumentException(
                        "Wrong message token, perhaps other side uses different list of middlewares (compression, encryption, auth, etc)");

                MessageType messageType = (MessageType)rpcMessage.ReadByte();
                switch (messageType)
                {
                    case MessageType.RpcRequest:
                        RemotingRequest remotingRequest = new RemotingRequest();
                        remotingRequest.MergeFrom(rpcMessage);
                        LogMessageReceived(remotingRequest);
                        RemotingRequest(remotingRequest);
                        break;
                    case MessageType.RpcResponse:
                        RemotingResponse remotingResponse = new RemotingResponse();
                        remotingResponse.MergeFrom(rpcMessage);
                        LogMessageReceived(remotingResponse);
                        RemotingResponse(remotingResponse);
                        break;
                    case MessageType.RpcResponseError:
                        RemotingResponseError remotingResponseError = new RemotingResponseError();
                        remotingResponseError.MergeFrom(rpcMessage);
                        LogMessageReceived(remotingResponseError);
                        RemotingResponseError(remotingResponseError);
                        break;
                    case MessageType.AuthenticateRequest:
                        AuthenticationRequest authenticationRequest = new AuthenticationRequest();
                        authenticationRequest.MergeFrom(rpcMessage);
                        LogMessageReceived(authenticationRequest);
                        AuthenticationRequest(authenticationRequest);
                        break;
                    case MessageType.AuthenticateResponse:
                        AuthenticationResponse authenticationResponse = new AuthenticationResponse();
                        authenticationResponse.MergeFrom(rpcMessage);
                        LogMessageReceived(authenticationResponse);
                        AuthenticationResponse(authenticationResponse);
                        break;
                    default:
                        throw new ArgumentException($"Wrong message type: {messageType}, perhaps other side uses different list of middlewares (compression, encryption, auth, etc)");
                }
            }
            catch (Exception outerException)
            {
                logger.Error($"{LogsSign} got an unhandled exception on {nameof(RpcSessionBase)}.{nameof(OnMessage)}(): {outerException}");
                connection.Close();
            }
        }
        
        private protected void LogMessageReceived(INeonMessage message)
        {
            this.logger.Trace($"{LogsSign} received {message}");
        }
        
        private protected virtual void RemotingRequest(RemotingRequest remotingRequest)
        {
            throw new NotSupportedException($"Got unexpected {nameof(RemotingRequest)}");
        }
        
        private protected virtual void RemotingResponse(RemotingResponse remotingResponse)
        {
            throw new NotSupportedException($"Got unexpected {nameof(RemotingResponse)}");
        }
        
        private protected virtual void RemotingResponseError(RemotingResponseError remotingResponseError)
        {
            throw new NotSupportedException($"Got unexpected {nameof(RemotingResponseError)}");
        }

        private protected virtual void AuthenticationRequest(AuthenticationRequest authenticationRequest)
        {
            throw new NotSupportedException($"Got unexpected {nameof(AuthenticationRequest)}");
        }
        
        private protected virtual void AuthenticationResponse(AuthenticationResponse authenticationResponse)
        {
            throw new NotSupportedException($"Got unexpected {nameof(AuthenticationResponse)}");
        }

        public override string ToString()
        {
            return $"{this.GetType().Name}[connection_id={connection.Id},endpoint={connection.RemoteEndpoint}]";
        }

        private protected string LogsSign => $"#{connection.Id} ({this.GetType().Name})";

        private protected Task SendNeonMessage(INeonMessage message)
        {
            return SendNeonMessage(message, DeliveryType.ReliableOrdered, UdpConnection.DEFAULT_CHANNEL);
        }

        private protected async Task SendNeonMessage(INeonMessage message, DeliveryType deliveryType, int channel)
        {
            logger.Trace($"{LogsSign} sending {message}");
            using (var rpcMessage = connection.CreateRpcMessage())
            {
                message.WriteTo(rpcMessage);
                await connection.SendMessage(rpcMessage, deliveryType, channel).ConfigureAwait(false);
            }
        }
    }
}