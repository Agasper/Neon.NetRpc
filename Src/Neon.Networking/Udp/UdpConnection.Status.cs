using System;
using System.Threading;
using System.Threading.Tasks;
using Neon.Networking.Messages;
using Neon.Networking.Udp.Events;
using Neon.Networking.Udp.Exceptions;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp
{
    public partial class UdpConnection
    {
        readonly TaskCompletionSource<object> connectingTcs;
        readonly TaskCompletionSource<object> disconnectingTcs;
        bool wasOpened;
        
        bool CheckStatusForDatagram(Datagram datagram, UdpConnectionStatus status, UdpConnectionStatus status2)
        {
            bool result = false;
            lock (connectionMutex)
                result = this.status == status || this.status == status2;
            if (!result)
            {
                logger.Trace(
                    $"#{Id} got {datagram.Type} in wrong connection status: {status}, expected: {status}. Dropping...");
                return false;
            }

            return true;
        }
        
        bool CheckStatusForDatagram(Datagram datagram, UdpConnectionStatus status)
        {
            bool result = false;
            UdpConnectionStatus status_;
            lock (connectionMutex)
            {
                status_ = this.status;
                result = status_ == status;
            }

            if (!result)
            {
                logger.Trace(
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

        bool ChangeStatus(UdpConnectionStatus status, Func<UdpConnectionStatus, bool> statusCheck, out UdpConnectionStatus oldStatus)
        {
            lock (connectionMutex)
            {
                oldStatus = this.status;
                if (oldStatus == status)
                    return false;
                if (!statusCheck(oldStatus))
                    return false;
                this.status = status;
            }

            this.logger.Info($"#{Id} changed status from {oldStatus} to {status}");
            this.lastStatusChange = DateTime.UtcNow;
            UpdateTimeoutDeadline();

            var statusChangedArgs = new ConnectionStatusChangedEventArgs(status, this);
            peer.Configuration.SynchronizeSafe(logger, $"{nameof(UdpConnection)}.{nameof(OnStatusChanged)}",
                (state) => OnStatusChanged(state as ConnectionStatusChangedEventArgs), statusChangedArgs);
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
            {
                throw new InvalidOperationException(
                    $"Couldn't connect, wrong status: {status}, expected {UdpConnectionStatus.InitialWaiting}");
            }

            logger.Info($"#{Id} is connecting to {EndPoint.EndPoint}");
            Datagram connectReq = Parent.CreateDatagramEmpty(MessageType.ConnectReq, serviceReliableChannel.Descriptor);;

            using (cancellationToken.Register(() =>
                   {
                       connectingTcs.TrySetCanceled();
                       this.CloseImmediately(DisconnectReason.ClosedByThisPeer);
                   }))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await serviceReliableChannel.SendDatagramAsync(connectReq).ConfigureAwait(false);
                await connectingTcs.Task.ConfigureAwait(false);
            }
        }

        void EndConnect()
        {
            lock (connectionMutex)
            {

                if (!ChangeStatus(UdpConnectionStatus.Connected,
                        status => (!this.IsClientConnection && status == UdpConnectionStatus.InitialWaiting)
                                  || (this.IsClientConnection && status == UdpConnectionStatus.Connecting)))
                {
                    return;
                }

                nextPingSend = DateTime.UtcNow.AddMilliseconds(Parent.Configuration.KeepAliveInterval);

                wasOpened = true;
                var openedArgs = new ConnectionOpenedEventArgs(this);
                logger.Info($"#{Id} opened");
                peer.Configuration.SynchronizeSafe(logger, $"{nameof(UdpConnection)}.{nameof(OnConnectionOpened)}",
                    (state) =>
                    {
                        var args = (ConnectionOpenedEventArgs) state;
                        OnConnectionOpened(args);
                    }, openedArgs);
                Parent.OnConnectionOpenedInternal(openedArgs);

            }
            
            if (IsClientConnection)
                ExpandMTU();

            connectingTcs.TrySetResult(null);
        }

        /// <summary>
        /// Start connection closing process
        /// </summary>
        public virtual Task CloseAsync()
        {
            return StartClose();
        }
        
        /// <summary>
        /// Drop the connection immediately, the remote host consider connection dead after timeout
        /// </summary>
        public void CloseImmediately()
        {
            this.CloseInternal(DisconnectReason.ClosedByThisPeer);
        }

        internal void CloseImmediately(DisconnectReason reason)
        {
            this.CloseInternal(reason);
        }

        void CloseInternal(DisconnectReason reason)
        {
            lock (connectionMutex)
            {
                if (!ChangeStatus(UdpConnectionStatus.Disconnected))
                    return;

                connectionCancellationToken.Cancel();

                if (wasOpened)
                {
                    logger.Info($"#{Id} closed ({reason})");
                    var args = new ConnectionClosedEventArgs(this, reason);
                    peer.Configuration.SynchronizeSafe(logger, $"{nameof(UdpConnection)}.{nameof(OnConnectionClosed)}",
                        (state) =>
                        {
                            var args_ = (ConnectionClosedEventArgs) state;
                            OnConnectionClosed(args_);
                        }, args);
                    Parent.OnConnectionClosedInternal(args);
                }
            }

            connectingTcs.TrySetException(ConnectionException.CreateFromReasonForConnect(reason));
            disconnectingTcs.TrySetResult(null);
        }


        async Task StartClose()
        {
            bool sendPacketRequired = true;

            lock (connectionMutex)
            {
                switch (status)
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
                        throw new InvalidOperationException($"Wrong status: {status}");
                }
            }

            if (sendPacketRequired)
            {
                Datagram disconnectReq = Parent.CreateDatagramEmpty(MessageType.DisconnectReq, serviceReliableChannel.Descriptor);
                await serviceReliableChannel.SendDatagramAsync(disconnectReq).ConfigureAwait(false);
            }

            await disconnectingTcs.Task.ConfigureAwait(false);
        }
    }
}