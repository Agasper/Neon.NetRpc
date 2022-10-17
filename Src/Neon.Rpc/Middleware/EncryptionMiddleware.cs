using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking.Cryptography;
using Neon.Networking.Messages;
using Neon.Networking.Udp;
using Neon.Networking.Udp.Messages;
using Neon.Rpc.Cryptography;
using Neon.Rpc.Net;
using Neon.Rpc.Net.Tcp;

namespace Middleware
{
    class EncryptionMiddleware : IMiddleware, IDisposable
    {
        DiffieHellmanImpl dh;
        ICipher cipher;
        TaskCompletionSource<object> tcs;
        CancellationToken cancellationToken;
        bool done;
        
        readonly IMiddlewareConnection connection;
        readonly ILogger logger;

        public EncryptionMiddleware(IMiddlewareConnection connection, ILogManager logManager)
        {
            this.logger = logManager.GetLogger(nameof(EncryptionMiddleware));
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            CheckCipher();

            this.cancellationToken = cancellationToken;
            using (cancellationToken.Register(() =>
                   {
                       tcs.TrySetException(new OperationCanceledException("Connection initialization cancelled"));
                   }))
            {
                if (connection.IsClientConnection)
                    await SendHandshakeRequest().ConfigureAwait(false);

                await tcs.Task.ConfigureAwait(false);
            }
        }


        public void Reset()
        {
            this.done = false;
            this.cipher?.Dispose();
            this.cipher = null;
            this.dh = null;
            this.tcs?.TrySetCanceled();
            this.tcs = null;
        }
        
        public void Set(ICipher cipher)
        {
            Reset();
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            this.dh = new DiffieHellmanImpl(cipher);
            this.tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        void CheckCipher()
        {
            if (this.cipher == null)
                throw new NullReferenceException("Cipher not set");
        }
        
        
        public void MiddlewareMessage(RawMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException($"{nameof(EncryptionMiddleware)} keys exchange is cancelled");
            
            try
            {
                if (dh.Status == DiffieHellmanImpl.DhStatus.None)
                {
                    RecvHandshakeRequest(message)
                        .ContinueWith((t, state) =>
                        {
                            tcs.TrySetException(
                                new InvalidOperationException($"Failed to proceed handshake request: {t.Exception}"));
                        }, connection, TaskContinuationOptions.OnlyOnFaulted);
                    return;
                }

                if (dh.Status == DiffieHellmanImpl.DhStatus.WaitingForServerMod)
                {
                    dh.RecvHandshakeResponse(message);
                    Done();
                    return;
                }

                throw new InvalidOperationException("Wrong state for middleware message");

            }
            catch (Exception e)
            {
                tcs.TrySetException(
                    new InvalidOperationException($"Handshake failed for {this.connection}. Closing... {e}"));
            }
            finally
            {
                message.Dispose();
            }
        }

        public RawMessage ReceiveMessage(RawMessage message)
        {
            if (!message.Encrypted)
                throw new InvalidOperationException("Got unencrypted message");

            RawMessage decryptedMessage = null;
            try
            {
                CheckCipher();
                decryptedMessage = message.Decrypt(cipher);
                logger.Trace($"#{connection.Id} decrypted {message} to {decryptedMessage}");
                return decryptedMessage;
            }
            catch (Exception)
            {
                decryptedMessage?.Dispose();
                throw;
            }
            finally
            {
                message.Dispose();
            }
        }

        public void Dispose()
        {
            Reset();
        }
        
        async Task SendHandshakeRequest()
        {
            using (var message = connection.CreateMessage())
            {
                dh.SendHandshakeRequest(message);
                await connection.SendMessageWithMiddlewaresAsync(message, DeliveryType.ReliableOrdered, UdpConnection.DEFAULT_CHANNEL).ConfigureAwait(false);
                logger.Debug($"#{connection.Id} secure handshake request sent!");
            }
        }
        
        async Task RecvHandshakeRequest(IRawMessage incomingMessage)
        {
            if (incomingMessage == null) 
                throw new ArgumentNullException(nameof(incomingMessage));
            logger.Debug($"#{connection.Id} got secure handshake request");
            using (var responseMessage = connection.CreateMessage())
            {
                dh.RecvHandshakeRequest(incomingMessage, responseMessage);
                await connection.SendMessageWithMiddlewaresAsync(responseMessage, DeliveryType.ReliableOrdered, UdpConnection.DEFAULT_CHANNEL).ConfigureAwait(false);
            }
            
            Done();
        }

        void Done()
        {
            CheckCipher();
            logger.Debug($"#{connection.Id} common key set!");
            done = true;
            tcs.TrySetResult(null);
        }

        public RawMessage SendMessage(RawMessage message)
        {
            CheckCipher();

            if (!done)
                return message;

            if (message.Encrypted)
                throw new InvalidOperationException("Got already encrypted message");

            RawMessage encryptedMessage = null;
            try
            {
                encryptedMessage = message.Encrypt(this.cipher);
                encryptedMessage.Position = 0;

                logger.Trace($"#{connection.Id} encrypted {message} to {encryptedMessage}");
                return encryptedMessage;
            }
            catch (Exception)
            {
                encryptedMessage?.Dispose();
                throw;
            }
            finally
            {
                message.Dispose();
            }
        }
    }
}