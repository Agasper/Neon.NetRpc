using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking.Messages;
using Neon.Networking.Tcp.Events;
using Neon.Networking.Tcp.Messages;
using Neon.Util.Pooling;

namespace Neon.Networking.Tcp
{
    public class TcpConnection : IDisposable
    {
        /// <summary>
        ///     Does this connection belongs to the client
        /// </summary>
        public bool IsClientConnection { get; private set; }

        /// <summary>
        ///     Was this connection disposed
        /// </summary>
        public bool Disposed => _disposed;

        /// <summary>
        ///     A user defined tag
        /// </summary>
        public virtual object Tag { get; set; }

        /// <summary>
        ///     Unique connection id
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        ///     In case of many simultaneously sends, they're queued. This is current queue size
        /// </summary>
        public int SendQueueSize => _sendQueueSize;

        /// <summary>
        ///     Connection remote endpoint
        /// </summary>
        public EndPoint RemoteEndpoint
        {
            get
            {
                try
                {
                    return _socket.RemoteEndPoint;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        ///     Is we still connected
        /// </summary>
        public bool Connected
        {
            get
            {
                if (_closed)
                    return false;
                Socket socket = _socket;
                if (socket == null)
                    return false;
                return socket.Connected;
            }
        }

        /// <summary>
        ///     Connection creation time
        /// </summary>
        public DateTime Started { get; private set; }

        /// <summary>
        ///     Token will be cancelled as soon connection is terminated
        /// </summary>
        public CancellationToken CancellationToken => _connectionCancellationToken.Token;

        /// <summary>
        ///     Parent peer
        /// </summary>
        public TcpPeer Parent { get; }

        /// <summary>
        ///     Connection statistics
        /// </summary>
        public TcpConnectionStatistics Statistics { get; }

        protected DateTime? LastKeepAliveRequestReceived { get; private set; }
        readonly TcpMessageHeaderFactory _awaitingMessageHeaderFactory;
        readonly CancellationTokenSource _connectionCancellationToken;
        readonly ConcurrentQueue<TcpDelayedMessage> _latencySimulationRecvQueue;
        readonly ConcurrentQueue<TcpDelayedMessage> _latencySimulationSendQueue;

        protected readonly ILogger _logger;

        readonly IRentedArray _recvBuffer;
        readonly IRentedArray _sendBuffer;
        readonly object _sendMutex = new object();
        readonly SemaphoreSlim _sendSemaphore;
        TcpMessageHeader _awaitingMessageHeader;
        RawMessage _awaitingNextMessage;
        bool _awaitingNextMessageHeaderValid;
        int _awaitingNextMessageWrote;

        volatile bool _closed;
        volatile bool _disposed;
        bool _keepAliveResponseGot;
        DateTime _lastKeepAliveSent;
        int _sendQueueSize;
        volatile Task _sendTask;

        Socket _socket;

        public TcpConnection(TcpPeer parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            _latencySimulationRecvQueue = new ConcurrentQueue<TcpDelayedMessage>();
            _latencySimulationSendQueue = new ConcurrentQueue<TcpDelayedMessage>();
            _connectionCancellationToken = new CancellationTokenSource();
            Statistics = new TcpConnectionStatistics();
            _sendSemaphore = new SemaphoreSlim(1, 1);
            _recvBuffer = parent.Configuration.MemoryManager.RentArray(parent.Configuration.ReceiveBufferSize);
            _sendBuffer = parent.Configuration.MemoryManager.RentArray(parent.Configuration.SendBufferSize);
            _awaitingMessageHeaderFactory = new TcpMessageHeaderFactory();
            Parent = parent;
            _logger = parent.Configuration.LogManager.GetLogger(typeof(TcpConnection));
            _logger.Meta["connection_endpoint"] = new RefLogLabel<TcpConnection>(this, v => v.RemoteEndpoint);
            _logger.Meta["connected"] = new RefLogLabel<TcpConnection>(this, s => s.Connected);
            _logger.Meta["closed"] = new RefLogLabel<TcpConnection>(this, s => s._closed);
            _logger.Meta["latency"] = new RefLogLabel<TcpConnection>(this, s =>
            {
                int? lat = s.Statistics.Latency;
                if (lat.HasValue)
                    return lat.Value;
                return "";
            });

            // return $"{nameof(TcpConnection)}[id={Id}, connected={Connected}, endpoint={RemoteEndpoint}]";
        }

        /// <summary>
        ///     Returns the memory used by this connection
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        internal void CheckParent(TcpPeer parent)
        {
            if (!ReferenceEquals(Parent, parent))
                throw new InvalidOperationException("This connection belongs to the another parent");
        }

        void CheckDisposed()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(TcpConnection));
        }

        internal void Init(long connectionId, Socket socket, bool isClientConnection)
        {
            CheckDisposed();

            IsClientConnection = isClientConnection;
            _logger.Meta["connection_id"] = connectionId;
            _keepAliveResponseGot = true;
            Id = connectionId;
            _socket = socket;
            _sendTask = Task.CompletedTask;
            _closed = false;
            Started = DateTime.UtcNow;
            _lastKeepAliveSent = DateTime.UtcNow;
            _logger.Info($"#{Id} initialized");

            InitVirtual();
            Parent.OnConnectionOpenedInternal(this);
        }

        protected virtual void InitVirtual()
        {
        }

        /// <summary>
        ///     Returns the memory used by this connection
        /// </summary>
        /// <param name="disposing">Whether we're disposing (true), or being called by the finalizer (false)</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _disposed = true;

            CloseInternal(null);

            if (_awaitingNextMessage != null)
            {
                _awaitingNextMessage.Dispose();
                _awaitingNextMessage = null;
            }

            _connectionCancellationToken?.Dispose();

            _sendSemaphore?.Dispose();

            _recvBuffer.Dispose();
            _sendBuffer.Dispose();

            _logger.Debug($"#{Id} disposed!");
        }

        void DestroySocket(int? timeout)
        {
            _logger.Debug($"#{Id} socket destroying...");
            if (timeout.HasValue)
                _socket.Close(timeout.Value);
            else
                _socket.Close();
            _socket.Dispose();
        }

        internal void StartReceive()
        {
            _socket.BeginReceive(_recvBuffer.Array, 0, _recvBuffer.Array.Length, SocketFlags.None, ReceiveCallback, null);
        }

        protected internal virtual void OnConnectionClosed(ConnectionClosedEventArgs args)
        {
        }

        protected internal virtual void OnConnectionOpened(ConnectionOpenedEventArgs args)
        {
        }

        /// <summary>
        ///     Closes the connection
        /// </summary>
        public virtual void Close()
        {
            CloseInternal(null);
        }

        void CloseInternal(Exception ex)
        {
            if (_closed)
                return;
            _closed = true;

            try
            {
                _logger.Trace($"#{Id} closing");

                try
                {
                    _connectionCancellationToken.Cancel(false);
                }
                catch (Exception cex)
                {
                    _logger.Error($"Unhandled exception on cancelling token: {cex}");
                }

                //https://docs.microsoft.com/en-gb/windows/win32/winsock/graceful-shutdown-linger-options-and-socket-closure-2
                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
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
                        _logger.Info($"#{Id} closed with socket exception {sex.ErrorCode}: {sex.Message}");
                    else
                        _logger.Info($"#{Id} closed with exception: {ex}");
                }
                else
                {
                    _logger.Info($"#{Id} closed!");
                }

                Parent.OnConnectionClosedInternal(this, ex);

                Dispose();
            }
            catch (Exception ex_)
            {
                _logger.Critical("Exception on connection close: " + ex_);
            }
        }

        internal void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                // if (socket == null || !socket.Connected)
                //     return;

                int bytesRead = _socket.EndReceive(result);

                if (bytesRead == 0)
                {
                    CloseInternal(null);
                    return;
                }

                Statistics.BytesIn(bytesRead);
                _logger.Trace($"#{Id} recv data {bytesRead} bytes");

                var recvBufferPos = 0;
                var counter = 0;

                while (recvBufferPos <= bytesRead)
                {
                    int bytesLeft = bytesRead - recvBufferPos;
                    if (!_awaitingNextMessageHeaderValid)
                    {
                        if (bytesLeft > 0)
                        {
                            _awaitingNextMessageHeaderValid = _awaitingMessageHeaderFactory.Write(
                                new ArraySegment<byte>(_recvBuffer.Array, recvBufferPos, bytesLeft), out int headerGotRead,
                                out _awaitingMessageHeader);
                            recvBufferPos += headerGotRead;
                            _logger.Trace($"#{Id} ReceiveCallback(): Read header {_awaitingMessageHeader}");
                        }
                        else
                        {
                            _logger.Trace($"#{Id} ReceiveCallback(): No bytes left for the header, exit");
                            break;
                        }

                        if (_awaitingNextMessageHeaderValid)
                        {
                            // if (awaitingMessageHeader.MessageSize > 0)
                            {
                                var newGuid = Guid.NewGuid();
                                _awaitingNextMessage = new RawMessage(Parent.Configuration.MemoryManager,
                                    _awaitingMessageHeader.MessageSize,
                                    _awaitingMessageHeader.Flags.HasFlag(TcpMessageFlagsEnum.Compressed),
                                    _awaitingMessageHeader.Flags.HasFlag(TcpMessageFlagsEnum.Encrypted),
                                    newGuid, false);
                            }
                            // else
                            //     awaitingNextMessage = null;
                            _awaitingNextMessageWrote = 0;
                            _logger.Trace($"#{Id} ReceiveCallback(): Creating awaiting message...");
                        }
                    }
                    else
                    {
                        if (_awaitingNextMessageWrote < _awaitingMessageHeader.MessageSize && bytesLeft > 0)
                        {
                            int toRead = bytesLeft;
                            if (toRead > _awaitingMessageHeader.MessageSize - _awaitingNextMessageWrote)
                                toRead = _awaitingMessageHeader.MessageSize - _awaitingNextMessageWrote;
                            if (toRead > 0)
                            {
                                _awaitingNextMessage.Write(_recvBuffer.Array, recvBufferPos, toRead);
                                _awaitingNextMessageWrote += toRead;
                                recvBufferPos += toRead;
                            }

                            _logger.Trace($"#{Id} ReceiveCallback(): Read {toRead} bytes in the message");
                        }
                        else if (_awaitingNextMessageWrote == _awaitingMessageHeader.MessageSize)
                        {
                            _logger.Trace(
                                $"#{Id} ReceiveCallback(): Message done {_awaitingNextMessageWrote}=={_awaitingMessageHeader.MessageSize} !");
                            Statistics.PacketIn();
                            RawMessage message = _awaitingNextMessage;
                            if (message != null)
                            {
                                message.Position = 0;
                                message.MakeReadOnly();
                            }
                            _awaitingNextMessage = null;
                            OnMessageReceivedInternalWithSimulation(new TcpMessage(_awaitingMessageHeader, message, CancellationToken.None));
                            _awaitingNextMessageWrote = 0;
                            _awaitingMessageHeaderFactory.Reset();
                            _awaitingNextMessageHeaderValid = false;
                        }
                        else if (bytesLeft == 0)
                        {
                            _logger.Trace($"#{Id} ReceiveCallback(): No bytes left for message, exit");
                            break;
                        }
                    }

                    //Infinite loop protection
                    if (counter++ > _recvBuffer.Array.Length / 2 + 100)
                    {
                        _logger.Critical($"#{Id} infinite loop in {this}");
                        throw new InvalidOperationException("Infinite loop");
                    }
                }

                StartReceive();
            }
            catch (ObjectDisposedException)
            {
            }
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
                _latencySimulationRecvQueue.Enqueue(new TcpDelayedMessage(message,
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
            _logger.Debug($"#{Id} received message {message}");

            if (message.Header.MessageType == TcpMessageTypeEnum.KeepAliveRequest)
            {
                LastKeepAliveRequestReceived = DateTime.UtcNow;
                SendMessageAsync(new TcpMessage(
                    new TcpMessageHeader(0, TcpMessageTypeEnum.KeepAliveResponse, TcpMessageFlagsEnum.None),
                    null, CancellationToken.None));
                message.Dispose();
                return;
            }

            if (message.Header.MessageType == TcpMessageTypeEnum.KeepAliveResponse)
            {
                Statistics.UpdateInstantLatency((int) (DateTime.UtcNow - _lastKeepAliveSent).TotalMilliseconds);
                _keepAliveResponseGot = true;
                message.Dispose();
                return;
            }

            var args = new MessageEventArgs(this, message.RawMessage);
            Parent.Configuration.SynchronizeSafe(_logger, $"{nameof(TcpConnection)}.{nameof(OnMessageReceived)}",
                state => OnMessageReceived(state as MessageEventArgs), args
            );
        }

        protected internal virtual void PollEvents()
        {
            if (!_closed && Connected && Parent.Configuration.KeepAliveEnabled)
            {
                TimeSpan timeSinceLastKeepAlive = DateTime.UtcNow - _lastKeepAliveSent;
                if (_keepAliveResponseGot)
                {
                    if (timeSinceLastKeepAlive.TotalMilliseconds > Parent.Configuration.KeepAliveInterval)
                        try
                        {
                            _keepAliveResponseGot = false;
                            _lastKeepAliveSent = DateTime.UtcNow;
                            SendMessageAsync(new TcpMessage(
                                new TcpMessageHeader(0, TcpMessageTypeEnum.KeepAliveRequest, TcpMessageFlagsEnum.None),
                                null, CancellationToken.None));
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn($"#{Id} failed to send keep alive request: {ex}");
                        }
                }
                else
                {
                    var latency = (int) timeSinceLastKeepAlive.TotalMilliseconds;
                    if (latency > Statistics.InstantLatency)
                        Statistics.UpdateInstantLatency(latency);

                    if (Parent.Configuration.KeepAliveTimeout > Timeout.Infinite &&
                        latency > Parent.Configuration.KeepAliveTimeout)
                    {
                        _logger.Debug($"#{Id} KeepAliveTimeout exceeded");
                        CloseInternal(new TimeoutException("KeepAliveTimeout exceeded"));
                    }
                }
            }

            Statistics.PollEvents();

            while (_latencySimulationSendQueue.Count > 0 && Connected)
                if (_latencySimulationSendQueue.TryPeek(out TcpDelayedMessage msg))
                {
                    if (DateTime.UtcNow >= msg.ReleaseTimestamp)
                    {
                        if (_latencySimulationSendQueue.TryDequeue(out TcpDelayedMessage _msg))
                            _ = SendMessageSkipSimulationAsync(_msg.Message)
                                .ContinueWith(_msg.Complete, TaskContinuationOptions.ExecuteSynchronously);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }

            while (_latencySimulationRecvQueue.Count > 0 && Connected)
                if (_latencySimulationRecvQueue.TryPeek(out TcpDelayedMessage msg))
                {
                    if (DateTime.UtcNow >= msg.ReleaseTimestamp)
                    {
                        _latencySimulationRecvQueue.TryDequeue(out TcpDelayedMessage _msg);
                        OnMessageReceivedInternal(msg.Message);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
        }

        protected virtual void OnMessageReceived(MessageEventArgs args)
        {
        }

        void CheckConnected()
        {
            if (!Connected)
                throw new InvalidOperationException("is not established");
        }

        /// <summary>
        ///     Sends the message
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ArgumentNullException">If message is null</exception>
        public virtual Task SendMessageAsync(IRawMessage message, CancellationToken cancellationToken)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (message.Length == 0)
                return Task.CompletedTask;
            var tcpMessage =
                new TcpMessage(TcpMessageHeader.FromMessage(message, TcpMessageTypeEnum.UserData), message, cancellationToken);
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
                _latencySimulationSendQueue.Enqueue(delayedMessage);
                return delayedMessage.GetTask();
            }

            return SendMessageSkipSimulationAsync(message);
        }

        async Task SendMessageSkipSimulationAsync(TcpMessage message)
        {
            message.CancellationToken.ThrowIfCancellationRequested();
            Task newSendTask = null;
            lock (_sendMutex)
            {
                Interlocked.Increment(ref _sendQueueSize);
                _sendTask = _sendTask.ContinueWith(
                        (task, msg) => { return SendMessageImmediatelyInternalAsync(msg as TcpMessage); }, 
                        message, message.CancellationToken,
                        TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                    .Unwrap();

                newSendTask = _sendTask;
            }

            await newSendTask.ConfigureAwait(false);
        }

        async Task SendMessageImmediatelyInternalAsync(TcpMessage message)
        {
            message.CancellationToken.ThrowIfCancellationRequested();
            await _sendSemaphore.WaitAsync(message.CancellationToken).ConfigureAwait(false);
            try
            {
                message.CancellationToken.ThrowIfCancellationRequested();
                if (!Connected)
                    return;

                // if (message.Header.MessageType == TcpMessageTypeEnum.KeepAliveRequest || 
                //     message.Header.MessageType == TcpMessageTypeEnum.KeepAliveResponse)
                //     logger.Trace($"#{Id} sending {message}");
                // else
                _logger.Debug($"#{Id} sending {message}");

                ArraySegment<byte> headerBytes = TcpMessageHeaderFactory.Build(message.Header);
                Buffer.BlockCopy(headerBytes.Array, headerBytes.Offset, _sendBuffer.Array, 0, headerBytes.Count);
                int bufferPosition = headerBytes.Count;
                var totalBytesSent = 0;
                var messageEof = false;

                if (message.RawMessage != null)
                    message.RawMessage.Position = 0;

                do
                {
                    message.CancellationToken.ThrowIfCancellationRequested();
                    int bufferFreeSpace = _sendBuffer.Array.Length - bufferPosition;
                    var messageLeftBytes = 0;
                    if (message.RawMessage != null)
                        messageLeftBytes = message.RawMessage.Length - message.RawMessage.Position;
                    int toCopy = bufferFreeSpace;
                    if (messageLeftBytes <= toCopy)
                    {
                        toCopy = messageLeftBytes;
                        messageEof = true;
                    }

                    if (toCopy > 0 && message.RawMessage != null)
                        toCopy = message.RawMessage.Read(_sendBuffer.Array, bufferPosition, toCopy);

                    bufferPosition += toCopy;

                    var bufferSendPosition = 0;

                    while (bufferSendPosition < bufferPosition)
                    {
                        message.CancellationToken.ThrowIfCancellationRequested();
                        int sent = await Task.Factory
                            .FromAsync(
                                _socket.BeginSend(_sendBuffer.Array, bufferSendPosition,
                                    bufferPosition - bufferSendPosition,
                                    SocketFlags.None, null, null), _socket.EndSend)
                            .ConfigureAwait(false);
                        _logger.Debug($"#{Id} sent {sent} bytes");
                        bufferSendPosition += sent;
                        totalBytesSent += sent;
                    }

                    bufferPosition = 0;
                } while (!messageEof);

                Statistics.PacketOut();
                Statistics.BytesOut(totalBytesSent);
            }
            catch (OperationCanceledException)
            {
                
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
                Interlocked.Decrement(ref _sendQueueSize);
                _sendSemaphore.Release();
            }
        }

        public override string ToString()
        {
            return $"{nameof(TcpConnection)}[id={Id}, connected={Connected}, endpoint={RemoteEndpoint}]";
        }
    }
}