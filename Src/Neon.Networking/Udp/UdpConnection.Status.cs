using System;
using System.Threading;
using System.Threading.Tasks;
using Neon.Networking.Udp.Events;
using Neon.Networking.Udp.Exceptions;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp
{
    public partial class UdpConnection
    {
        readonly TaskCompletionSource<object> _connectingTcs;
        readonly TaskCompletionSource<object> _disconnectingTcs;
        bool _wasOpened;

        bool CheckStatusForDatagram(Datagram datagram, UdpConnectionStatus status, UdpConnectionStatus status2)
        {
            var result = false;
            lock (_connectionMutex)
            {
                result = _status == status || _status == status2;
            }

            if (!result)
            {
                _logger.Trace(
                    $"#{Id} got {datagram.Type} in wrong connection status: {status}, expected: {status}. Dropping...");
                return false;
            }

            return true;
        }

        bool CheckStatusForDatagram(Datagram datagram, UdpConnectionStatus status)
        {
            var result = false;
            UdpConnectionStatus status_;
            lock (_connectionMutex)
            {
                status_ = _status;
                result = status_ == status;
            }

            if (!result)
            {
                _logger.Trace(
                    $"#{Id} got {datagram.Type} in wrong connection status: {status_}, expected: {status}. Dropping...");
                return false;
            }

            return true;
        }

        bool ChangeStatus(UdpConnectionStatus status)
        {
            return ChangeStatus(status, status_ => true);
        }

        bool ChangeStatus(UdpConnectionStatus status, out UdpConnectionStatus oldStatus)
        {
            return ChangeStatus(status, status_ => true, out oldStatus);
        }

        bool ChangeStatus(UdpConnectionStatus status, Func<UdpConnectionStatus, bool> statusCheck)
        {
            return ChangeStatus(status, statusCheck, out _);
        }

        bool ChangeStatus(UdpConnectionStatus status, Func<UdpConnectionStatus, bool> statusCheck,
            out UdpConnectionStatus oldStatus)
        {
            lock (_connectionMutex)
            {
                oldStatus = _status;
                if (oldStatus == status)
                    return false;
                if (!statusCheck(oldStatus))
                    return false;
                _status = status;
            }

            _logger.Info($"#{Id} changed status from {oldStatus} to {status}");
            _lastStatusChange = DateTime.UtcNow;
            UpdateTimeoutDeadline();

            var statusChangedArgs = new ConnectionStatusChangedEventArgs(status, this);
            Parent.Configuration.SynchronizeSafe(_logger, $"{nameof(UdpConnection)}.{nameof(OnStatusChanged)}",
                state => OnStatusChanged(state as ConnectionStatusChangedEventArgs), statusChangedArgs);
            Parent.OnConnectionStatusChangedInternal(statusChangedArgs);

            return true;
        }

        internal Task Connect(CancellationToken cancellationToken)
        {
            return StartConnect(cancellationToken);
        }

        async Task StartConnect(CancellationToken cancellationToken)
        {
            if (!ChangeStatus(UdpConnectionStatus.Connecting, status => status == UdpConnectionStatus.InitialWaiting))
                throw new InvalidOperationException(
                    $"Couldn't connect, wrong status: {_status}, expected {UdpConnectionStatus.InitialWaiting}");

            _logger.Info($"#{Id} is connecting to {_udpNetEndpoint._EndPoint}");
            Datagram connectReq =
                Parent.CreateDatagramEmpty(MessageType.ConnectReq, _serviceReliableChannel.Descriptor);
            ;

            using (cancellationToken.Register(() =>
                   {
                       _connectingTcs.TrySetCanceled();
                       CloseImmediately(DisconnectReason.ClosedByThisPeer);
                   }))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _serviceReliableChannel.SendDatagramAsync(connectReq, CancellationToken).ConfigureAwait(false);
                await _connectingTcs.Task.ConfigureAwait(false);
            }
        }

        void EndConnect()
        {
            lock (_connectionMutex)
            {
                if (!ChangeStatus(UdpConnectionStatus.Connected,
                        status => (!IsClientConnection && status == UdpConnectionStatus.InitialWaiting)
                                  || (IsClientConnection && status == UdpConnectionStatus.Connecting)))
                    return;

                _nextPingSend = DateTime.UtcNow.AddMilliseconds(Parent.Configuration.KeepAliveInterval);

                _wasOpened = true;
                var openedArgs = new ConnectionOpenedEventArgs(this);
                _logger.Info($"#{Id} opened");
                Parent.Configuration.SynchronizeSafe(_logger, $"{nameof(UdpConnection)}.{nameof(OnConnectionOpened)}",
                    state =>
                    {
                        var args = (ConnectionOpenedEventArgs) state;
                        OnConnectionOpened(args);
                    }, openedArgs);
                Parent.OnConnectionOpenedInternal(openedArgs);
            }

            if (IsClientConnection)
                ExpandMTU();

            _connectingTcs.TrySetResult(null);
        }

        /// <summary>
        ///     Start connection closing process
        /// </summary>
        public virtual Task CloseAsync()
        {
            return StartClose();
        }

        /// <summary>
        ///     Drop the connection immediately, the remote host consider connection dead after timeout
        /// </summary>
        public void CloseImmediately()
        {
            CloseInternal(DisconnectReason.ClosedByThisPeer);
        }

        internal void CloseImmediately(DisconnectReason reason)
        {
            CloseInternal(reason);
        }

        void CloseInternal(DisconnectReason reason)
        {
            lock (_connectionMutex)
            {
                if (!ChangeStatus(UdpConnectionStatus.Disconnected))
                    return;

                _connectionCancellationToken.Cancel();

                if (_wasOpened)
                {
                    _logger.Info($"#{Id} closed ({reason})");
                    var args = new ConnectionClosedEventArgs(this, reason);
                    Parent.Configuration.SynchronizeSafe(_logger,
                        $"{nameof(UdpConnection)}.{nameof(OnConnectionClosed)}",
                        state =>
                        {
                            var args_ = (ConnectionClosedEventArgs) state;
                            OnConnectionClosed(args_);
                        }, args);
                    Parent.OnConnectionClosedInternal(args);
                }
            }

            _connectingTcs.TrySetException(ConnectionException.CreateFromReasonForConnect(reason));
            _disconnectingTcs.TrySetResult(null);
        }


        async Task StartClose()
        {
            var sendPacketRequired = true;

            lock (_connectionMutex)
            {
                switch (_status)
                {
                    case UdpConnectionStatus.InitialWaiting:
                        ChangeStatus(UdpConnectionStatus.Disconnected);
                        return;
                    case UdpConnectionStatus.Connecting:
                    case UdpConnectionStatus.Connected:
                        ChangeStatus(UdpConnectionStatus.Disconnecting);
                        sendPacketRequired = true;
                        break;
                    case UdpConnectionStatus.Disconnecting:
                        sendPacketRequired = false;
                        break;
                    case UdpConnectionStatus.Disconnected:
                        return;
                    default:
                        throw new InvalidOperationException($"Wrong status: {_status}");
                }
            }

            if (sendPacketRequired)
            {
                Datagram disconnectReq =
                    Parent.CreateDatagramEmpty(MessageType.DisconnectReq, _serviceReliableChannel.Descriptor);
                await _serviceReliableChannel.SendDatagramAsync(disconnectReq, CancellationToken).ConfigureAwait(false);
            }

            await _disconnectingTcs.Task.ConfigureAwait(false);
        }
    }
}