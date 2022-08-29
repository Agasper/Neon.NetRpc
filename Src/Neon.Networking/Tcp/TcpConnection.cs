using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Neon.Networking.Tcp.Events;
using Neon.Networking.Tcp.Messages;
using Neon.Logging;
using Neon.Networking.Messages;

namespace Neon.Networking.Tcp
{
    public class TcpConnection : IDisposable
    {
        /// <summary>
        /// Does this connection belongs to the client
        /// </summary>
        public bool IsClientConnection { get; private set; }
        /// <summary>
        /// Was this connection disposed 
        /// </summary>
        public bool Disposed => disposed;
        /// <summary>
        /// A user defined tag
        /// </summary>
        public virtual object Tag { get; set; }
        /// <summary>
        /// Unique connection id
        /// </summary>
        public long Id { get; private set; }
        /// <summary>
        /// In case of many simultaneously sends, they're queued. This is current queue size
        /// </summary>
        public int SendQueueSize => sendQueueSize;
        /// <summary>
        /// Connection remote endpoint
        /// </summary>
        public EndPoint RemoteEndpoint
        {
            get
            {
                try
                {
                    return socket.RemoteEndPoint;
                }
                catch
                {
                    return null;
                }
            }
        }
        /// <summary>
        /// Is we still connected
        /// </summary>
        public bool Connected
        {
            get
            {
                if (closed)
                    return false;
                var socket_ = this.socket;
                if (socket_ == null)
                    return false;
                return socket_.Connected;
            }
        }
        /// <summary>
        /// Connection creation time
        /// </summary>
        public DateTime Started { get; private set; }
        /// <summary>
        /// Token will be cancelled as soon connection is terminated
        /// </summary>
        public CancellationToken CancellationToken => connectionCancellationToken.Token;
        /// <summary>
        /// Parent peer
        /// </summary>
        public TcpPeer Parent { get; private set; }
        /// <summary>
        /// Connection statistics
        /// </summary>
        public TcpConnectionStatistics Statistics { get; private set; }

        protected DateTime? LastKeepAliveRequestReceived { get; private set; }

        Socket socket;
        readonly TcpMessageHeaderFactory awaitingMessageHeaderFactory;
        TcpMessageHeader awaitingMessageHeader;
        RawMessage awaitingNextMessage;
        bool awaitingNextMessageHeaderValid;
        int awaitingNextMessageWrote;
        int sendQueueSize;
        DateTime lastKeepAliveSent;
        bool keepAliveResponseGot;
        readonly SemaphoreSlim sendSemaphore;

        volatile bool closed;
        volatile bool disposed;
        volatile Task sendTask;
        readonly object sendMutex = new object();

        protected readonly ILogger logger;
        
        readonly byte[] recvBuffer;
        readonly byte[] sendBuffer;
        readonly ConcurrentQueue<TcpDelayedMessage> latencySimulationRecvQueue;
        readonly ConcurrentQueue<TcpDelayedMessage> latencySimulationSendQueue;
        readonly CancellationTokenSource connectionCancellationToken;

        public TcpConnection(TcpPeer parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            this.latencySimulationRecvQueue = new ConcurrentQueue<TcpDelayedMessage>();
            this.latencySimulationSendQueue = new ConcurrentQueue<TcpDelayedMessage>();
            this.connectionCancellationToken = new CancellationTokenSource();
            this.Statistics = new TcpConnectionStatistics();
            this.sendSemaphore = new SemaphoreSlim(1, 1);
            this.recvBuffer = parent.Configuration.MemoryManager.RentArray(parent.Configuration.ReceiveBufferSize);
            this.sendBuffer = parent.Configuration.MemoryManager.RentArray(parent.Configuration.SendBufferSize);
            this.awaitingMessageHeaderFactory = new TcpMessageHeaderFactory();
            this.Parent = parent;
            this.logger = parent.Configuration.LogManager.GetLogger(nameof(TcpConnection));
            this.logger.Meta["kind"] = this.GetType().Name;
            this.logger.Meta["connection_endpoint"] = new RefLogLabel<TcpConnection>(this, v => v.RemoteEndpoint);
            this.logger.Meta["connected"] = new RefLogLabel<TcpConnection>(this, s => s.Connected);
            this.logger.Meta["closed"] = new RefLogLabel<TcpConnection>(this, s => s.closed);
            this.logger.Meta["latency"] = new RefLogLabel<TcpConnection>(this, s =>
            {
                var lat = s.Statistics.Latency;
                if (lat.HasValue)
                    return lat.Value;
                else
                    return "";
            });

            // return $"{nameof(TcpConnection)}[id={Id}, connected={Connected}, endpoint={RemoteEndpoint}]";
        }

        internal void CheckParent(TcpPeer parent)
        {
            if (!ReferenceEquals(this.Parent, parent))
                throw new InvalidOperationException($"This connection belongs to the another parent");
        }

        void CheckDisposed()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(TcpConnection));   
        }

        internal void Init(long connectionId, Socket socket, bool isClientConnection)
        {
            CheckDisposed();

            this.IsClientConnection = isClientConnection;
            this.logger.Meta["connection_id"] = connectionId;
            this.keepAliveResponseGot = true;
            this.Id = connectionId;
            this.socket = socket;
            this.sendTask = Task.CompletedTask;
            this.closed = false;
            this.Started = DateTime.UtcNow;
            this.lastKeepAliveSent = DateTime.UtcNow;
            logger.Info($"#{Id} initialized");
            
            InitVirtual();
            Parent.OnConnectionOpenedInternal(this);
        }

        protected virtual void InitVirtual()
        {
            
        }

        /// <summary>
        /// Returns the memory used by this connection
        /// </summary>
        /// <param name="disposing">Whether we're disposing (true), or being called by the finalizer (false)</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            disposed = true;

            CloseInternal(null);

            if (this.awaitingNextMessage != null)
            {
                this.awaitingNextMessage.Dispose();
                this.awaitingNextMessage = null;
            }
            
            this.connectionCancellationToken?.Dispose();

            sendSemaphore?.Dispose();
            
            this.Parent.Configuration.MemoryManager.ReturnArray(this.recvBuffer);
            this.Parent.Configuration.MemoryManager.ReturnArray(this.sendBuffer);

            logger.Debug($"#{Id} disposed!");
        }

        /// <summary>
        /// Returns the memory used by this connection
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        void DestroySocket(int? timeout)
        {
            logger.Debug($"#{Id} socket destroying...");
            if (timeout.HasValue)
                socket.Close(timeout.Value);
            else
                socket.Close();
            socket.Dispose();
        }

        internal void StartReceive()
        {
            socket.BeginReceive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None, ReceiveCallback, null);
        }

        protected internal virtual void OnConnectionClosed(ConnectionClosedEventArgs args)
        {

        }
        
        protected internal virtual void OnConnectionOpened(ConnectionOpenedEventArgs args)
        {

        }

        /// <summary>
        /// Close the connection
        /// </summary>
        public virtual void Close()
        {
            CloseInternal(null);
        }
        
        void CloseInternal(Exception ex)
        {
            if (closed)
                return;
            closed = true;
            
            try
            {
                logger.Trace($"#{Id} closing");

                try
                {
                    connectionCancellationToken.Cancel(false);
                }
                catch (Exception cex)
                {
                    logger.Error($"Unhandled exception on cancelling token: {cex}");
                }

                //https://docs.microsoft.com/en-gb/windows/win32/winsock/graceful-shutdown-linger-options-and-socket-closure-2
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException)
                {
                }
                finally
                {
                    DestroySocket(null);
                }

                if (ex != null)
                {
                    if (ex is SocketException sex)
                        logger.Info($"#{Id} closed with socket exception {sex.ErrorCode}: {sex.Message}");
                    else
                        logger.Info($"#{Id} closed with exception: {ex}");
                }
                else
                    logger.Info($"#{Id} closed!");

                Parent.OnConnectionClosedInternal(this, ex);
                
                this.Dispose();
            }
            catch (Exception ex_)
            {
                logger.Critical("Exception on connection close: " + ex_);
            }
        }

        internal void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                // if (socket == null || !socket.Connected)
                //     return;

                int bytesRead = socket.EndReceive(result);

                if (bytesRead == 0)
                {
                    CloseInternal(null);
                    return;
                }

                Statistics.BytesIn(bytesRead);
                logger.Trace($"#{Id} recv data {bytesRead} bytes");

                int recvBufferPos = 0;
                int counter = 0;

                while (recvBufferPos <= bytesRead)
                {
                    int bytesLeft = bytesRead - recvBufferPos;
                    if (!awaitingNextMessageHeaderValid)
                    {
                        if (bytesLeft > 0)
                        {
                            awaitingNextMessageHeaderValid = awaitingMessageHeaderFactory.Write(
                                new ArraySegment<byte>(recvBuffer, recvBufferPos, bytesLeft), out int headerGotRead,
                                out awaitingMessageHeader);
                            recvBufferPos += headerGotRead;
                            logger.Trace($"#{Id} ReceiveCallback(): Read header {awaitingMessageHeader}");
                        }
                        else
                        {
                            logger.Trace($"#{Id} ReceiveCallback(): No bytes left for the header, exit");
                            break;
                        }

                        if (awaitingNextMessageHeaderValid)
                        {
                            if (awaitingMessageHeader.MessageSize > 0)
                            {
                                Guid newGuid = Guid.NewGuid();
                                awaitingNextMessage = new RawMessage(Parent.Configuration.MemoryManager,
                                    Parent.Configuration.MemoryManager.GetStream(awaitingMessageHeader.MessageSize,
                                        newGuid),
                                    awaitingMessageHeader.Flags.HasFlag(TcpMessageFlagsEnum.Compressed),
                                    awaitingMessageHeader.Flags.HasFlag(TcpMessageFlagsEnum.Encrypted),
                                    newGuid);
                            }
                            else
                                awaitingNextMessage = null;
                            awaitingNextMessageWrote = 0;
                            logger.Trace($"#{Id} ReceiveCallback(): Creating awaiting message...");
                        }
                    }
                    else
                    {
                        if (awaitingNextMessageWrote < awaitingMessageHeader.MessageSize && bytesLeft > 0)
                        {
                            int toRead = bytesLeft;
                            if (toRead > awaitingMessageHeader.MessageSize - awaitingNextMessageWrote)
                                toRead = awaitingMessageHeader.MessageSize - awaitingNextMessageWrote;
                            if (toRead > 0)
                            {
                                awaitingNextMessage.Write(recvBuffer, recvBufferPos, toRead);
                                awaitingNextMessageWrote += toRead;
                                recvBufferPos += toRead;
                            }
                            logger.Trace($"#{Id} ReceiveCallback(): Read {toRead} bytes in the message");
                        }
                        else if (awaitingNextMessageWrote == awaitingMessageHeader.MessageSize)
                        {
                            logger.Trace($"#{Id} ReceiveCallback(): Message done {awaitingNextMessageWrote}=={awaitingMessageHeader.MessageSize} !");
                            Statistics.PacketIn();
                            var message = awaitingNextMessage;
                            if (message != null)
                                message.Position = 0;
                            awaitingNextMessage = null;
                            OnMessageReceivedInternalWithSimulation(new TcpMessage(awaitingMessageHeader, message));
                            awaitingNextMessageWrote = 0;
                            awaitingMessageHeaderFactory.Reset();
                            awaitingNextMessageHeaderValid = false;
                        }
                        else if (bytesLeft == 0)
                        {
                            logger.Trace($"#{Id} ReceiveCallback(): No bytes left for message, exit");
                            break;
                        }
                    }

                    //Infinite loop protection
                    if (counter++ > recvBuffer.Length / 2 + 100)
                    {
                        logger.Critical($"#{Id} infinite loop in {this}");
                        throw new InvalidOperationException("Infinite loop");
                    }
                }

                StartReceive();

            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                CloseInternal(ex);
            }
        }

        void OnMessageReceivedInternalWithSimulation(TcpMessage message)
        {
            if (Parent.Configuration.ConnectionSimulation != null)
            {
                int delay = Parent.Configuration.ConnectionSimulation.GetHalfDelay();
                latencySimulationRecvQueue.Enqueue(new TcpDelayedMessage(message,
                    DateTime.UtcNow.AddMilliseconds(delay)));
                return;
            }

            OnMessageReceivedInternal(message);
        }

        void OnMessageReceivedInternal(TcpMessage message)
        {
            // if (message.Header.MessageType == TcpMessageTypeEnum.KeepAliveRequest ||
            //         message.Header.MessageType == TcpMessageTypeEnum.KeepAliveResponse)
            //     logger.Trace($"#{Id} recv message {message}");
            // else
                logger.Debug($"#{Id} received message {message}");

            if (message.Header.MessageType == TcpMessageTypeEnum.KeepAliveRequest)
            {
                LastKeepAliveRequestReceived = DateTime.UtcNow;
                SendMessageAsync(new TcpMessage(new TcpMessageHeader(0, TcpMessageTypeEnum.KeepAliveResponse, TcpMessageFlagsEnum.None),
                    null));
                message.Dispose();
                return;
            }

            if (message.Header.MessageType == TcpMessageTypeEnum.KeepAliveResponse)
            {
                Statistics.UpdateInstantLatency((int)(DateTime.UtcNow - this.lastKeepAliveSent).TotalMilliseconds);
                keepAliveResponseGot = true;
                message.Dispose();
                return;
            }

            var args = new MessageEventArgs(this, message.RawMessage);
            Parent.Configuration.SynchronizeSafe(logger, $"{nameof(TcpConnection)}.{nameof(OnMessageReceived)}",
                (state) => OnMessageReceived(state as MessageEventArgs), args
            );
        }

        protected internal virtual void PollEventsInternal()
        {
            // TimeSpan timeSinceLastKeepAlive = DateTime.UtcNow - this.lastKeepAliveSent;
            // if (timeSinceLastKeepAlive.TotalMilliseconds > Parent.Configuration.KeepAliveTimeout / 2f)
            // {
            //     Console.WriteLine(closed);
            //     Console.WriteLine(Connected);
            //     Console.WriteLine(keepAliveResponseGot);
            //     Console.WriteLine("WARNING !!!");
            // }

            if (!closed && Connected && Parent.Configuration.KeepAliveEnabled)
            {
                TimeSpan timeSinceLastKeepAlive = DateTime.UtcNow - this.lastKeepAliveSent;
                if (keepAliveResponseGot)
                {
                    if (timeSinceLastKeepAlive.TotalMilliseconds > Parent.Configuration.KeepAliveInterval)
                    {
                        try
                        {
                            SendMessageAsync(new TcpMessage(
                                new TcpMessageHeader(0, TcpMessageTypeEnum.KeepAliveRequest, TcpMessageFlagsEnum.None),
                                null));
                            
                            keepAliveResponseGot = false;
                            this.lastKeepAliveSent = DateTime.UtcNow;
                        }
                        catch (Exception ex)
                        {
                            logger.Warn($"#{Id} failed to send keep alive request: {ex}");
                        }
                    }
                }
                else
                {
                    int latency = (int)timeSinceLastKeepAlive.TotalMilliseconds;
                    if (latency > Statistics.InstantLatency)
                        Statistics.UpdateInstantLatency(latency);
                    
                    if (Parent.Configuration.KeepAliveTimeout > Timeout.Infinite && latency > Parent.Configuration.KeepAliveTimeout)
                    {
                        logger.Debug($"#{Id} KeepAliveTimeout exceeded");
                        CloseInternal(new TimeoutException("KeepAliveTimeout exceeded"));
                    }
                }
            }

            Statistics.PollEvents();
            
            while (latencySimulationSendQueue.Count > 0 && Connected)
            {
                if (latencySimulationSendQueue.TryPeek(out TcpDelayedMessage msg))
                {
                    if (DateTime.UtcNow >= msg.ReleaseTimestamp)
                    {
                        if (latencySimulationSendQueue.TryDequeue(out TcpDelayedMessage _msg))
                            _ = SendMessageSkipSimulationAsync(_msg.Message)
                                .ContinueWith(_msg.Complete, TaskContinuationOptions.ExecuteSynchronously);
                    }
                    else
                        break;
                }
                else
                    break;
            }

            while (latencySimulationRecvQueue.Count > 0 && Connected)
            {
                if (latencySimulationRecvQueue.TryPeek(out TcpDelayedMessage msg))
                {
                    if (DateTime.UtcNow >= msg.ReleaseTimestamp)
                    {
                        latencySimulationRecvQueue.TryDequeue(out TcpDelayedMessage _msg);
                        OnMessageReceivedInternal(msg.Message);
                    }
                    else
                        break;
                }
                else
                    break;
            }
        }

        protected virtual void OnMessageReceived(MessageEventArgs args)
        {

        }

        void CheckConnected()
        {
            if (!Connected)
                throw new InvalidOperationException($"is not established");
        }

        /// <summary>
        /// Sends the message
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <exception cref="ArgumentNullException">If message is null</exception>
        public virtual Task SendMessageAsync(RawMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (message.Length == 0)
                return Task.CompletedTask;
            var tcpMessage = new TcpMessage(TcpMessageHeader.FromMessage(message, TcpMessageTypeEnum.UserData), message);
            return SendMessageAsync(tcpMessage);
        }

        Task SendMessageAsync(TcpMessage message)
        {
            CheckConnected();
            
            if (Parent.Configuration.ConnectionSimulation != null)
            {
                int delay = Parent.Configuration.ConnectionSimulation.GetHalfDelay();
                var delayedMessage =
                    new TcpDelayedMessage(message,
                        DateTime.UtcNow.AddMilliseconds(delay));
                latencySimulationSendQueue.Enqueue(delayedMessage);
                return delayedMessage.GetTask();
            }

            return SendMessageSkipSimulationAsync(message);
        }

        async Task SendMessageSkipSimulationAsync(TcpMessage message)
        {
            Task newSendTask = null;
            lock (sendMutex)
            {
                Interlocked.Increment(ref sendQueueSize);
                sendTask = sendTask.ContinueWith(
                        (task, msg) =>
                        {
                            return SendMessageInternalAsync(msg as TcpMessage);
                        }, message, TaskContinuationOptions.ExecuteSynchronously)
                    .Unwrap();

                newSendTask = sendTask;
            }

            await newSendTask.ConfigureAwait(false);
        }

        async Task SendMessageInternalAsync(TcpMessage message)
        {
            await sendSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!Connected)
                    return;

                // if (message.Header.MessageType == TcpMessageTypeEnum.KeepAliveRequest || 
                //     message.Header.MessageType == TcpMessageTypeEnum.KeepAliveResponse)
                //     logger.Trace($"#{Id} sending {message}");
                // else
                logger.Debug($"#{Id} sending {message}");

                var headerBytes = TcpMessageHeaderFactory.Build(message.Header);
                Buffer.BlockCopy(headerBytes.Array, headerBytes.Offset, sendBuffer, 0, headerBytes.Count);
                int bufferPosition = headerBytes.Count;
                int totalBytesSent = 0;
                bool messageEof = false;

                if (message.RawMessage != null)
                    message.RawMessage.Position = 0;

                do
                {
                    int bufferFreeSpace = sendBuffer.Length - bufferPosition;
                    int messageLeftBytes = 0;
                    if (message.RawMessage != null)
                        messageLeftBytes = message.RawMessage.Length - message.RawMessage.Position;
                    int toCopy = bufferFreeSpace;
                    if (messageLeftBytes <= toCopy)
                    {
                        toCopy = messageLeftBytes;
                        messageEof = true;
                    }

                    if (toCopy > 0 && message.RawMessage != null)
                        toCopy = message.RawMessage.Read(sendBuffer, bufferPosition, toCopy);

                    bufferPosition += toCopy;

                    int bufferSendPosition = 0;

                    while (bufferSendPosition < bufferPosition)
                    {
                        int sent = await Task.Factory
                            .FromAsync(
                                socket.BeginSend(sendBuffer, bufferSendPosition, bufferPosition - bufferSendPosition,
                                    SocketFlags.None, null, null), socket.EndSend)
                            .ConfigureAwait(false);
                        logger.Trace($"#{Id} sent {sent} bytes");
                        bufferSendPosition += sent;
                        totalBytesSent += sent;
                    }

                    bufferPosition = 0;

                } while (!messageEof);

                Statistics.PacketOut();
                Statistics.BytesOut(totalBytesSent);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                CloseInternal(ex);
            }
            finally
            {
                Interlocked.Decrement(ref sendQueueSize);
                sendSemaphore.Release();
            }
        }

        public override string ToString()
        {
            return $"{nameof(TcpConnection)}[id={Id}, connected={Connected}, endpoint={RemoteEndpoint}]";
        }
    }
}
