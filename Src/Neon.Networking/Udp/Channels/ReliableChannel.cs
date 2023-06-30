using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking.Udp.Messages;
using Neon.Util;

namespace Neon.Networking.Udp.Channels
{
    class ReliableChannel : ChannelBase
    {
        const int START_WINDOW_SIZE = 64;

        readonly bool _ordered;
        readonly bool[] _recvEarlyReceived;
        readonly Datagram[] _recvWithheld;
        readonly Queue<DelayedPacket> _sendDelayedPackets;
        readonly PendingPacket[] _sendPendingPackets;
        readonly int _windowSize;

        int _ackWindowStart;
        bool _disposed;

        int _recvWindowStart;

        public ReliableChannel(ILogManager logManager,
            ChannelDescriptor descriptor, IChannelConnection connection, bool ordered)
            : base(logManager, descriptor, connection)
        {
            _ordered = ordered;
            _recvWindowStart = 0;
            _ackWindowStart = 0;
            _windowSize = START_WINDOW_SIZE;
            if (ordered)
                _recvWithheld = new Datagram[START_WINDOW_SIZE];
            else
                _recvEarlyReceived = new bool[START_WINDOW_SIZE];
            _sendPendingPackets = new PendingPacket[START_WINDOW_SIZE];
            _sendDelayedPackets = new Queue<DelayedPacket>();
        }

        public override void Dispose()
        {
            if (!_disposed)
                return;
            _disposed = true;
            lock (_channelMutex)
            {
                if (_recvWithheld != null)
                    for (var i = 0; i < _recvWithheld.Length; i++)
                    {
                        Datagram record = _recvWithheld[i];
                        if (record != null)
                            record.Dispose();
                        _recvWithheld[i] = null;
                    }

                for (var i = 0; i < _sendPendingPackets.Length; i++) _sendPendingPackets[i].Clear();

                foreach (DelayedPacket delayedPacket in _sendDelayedPackets) delayedPacket.Dispose();
            }
        }


        void OnAckReceived(Datagram datagram)
        {
            try
            {
                lock (_channelMutex)
                {
                    int relate = RelativeSequenceNumber(datagram.Sequence, _ackWindowStart);
                    if (relate < 0)
                    {
                        _logger.Trace($"{SignForLogs} got duplicate/late ack");
                        return; //late/duplicate ack
                    }

                    if (relate == 0)
                    {
                        _logger.Trace($"{SignForLogs} got ack {datagram} just in time, clearing pending datagrams...");
                        //connection.UpdateLatency(sendPendingPackets[ackWindowStart % WINDOW_SIZE].GetDelay());

                        var delayedPacketsCounter = 0;

                        _sendPendingPackets[_ackWindowStart % _windowSize].Clear();
                        _ackWindowStart = (_ackWindowStart + 1) % MAX_SEQUENCE;
                        delayedPacketsCounter++;

                        while (_sendPendingPackets[_ackWindowStart % _windowSize].AckReceived)
                        {
                            _logger.Trace(
                                $"{SignForLogs} clearing early pending {_sendPendingPackets[_ackWindowStart % _windowSize].Datagram}");
                            _sendPendingPackets[_ackWindowStart % _windowSize].Clear();
                            _ackWindowStart = (_ackWindowStart + 1) % MAX_SEQUENCE;
                            delayedPacketsCounter++;
                        }

                        TrySendDelayedPackets(delayedPacketsCounter);
                        return;
                    }

                    //Probably gap in ack sequence need faster message resend
                    //int curSequence = datagram.Sequence;
                    //do
                    //{
                    //    curSequence--;
                    //    if (curSequence < 0)
                    //        curSequence = MAX_SEQUENCE - 1;

                    //    int slot = curSequence % WINDOW_SIZE;
                    //    if (!ackReceived[slot])
                    //    {
                    //        if (sendPendingPackets[slot].ReSendNum == 1)
                    //        {
                    //            sendPendingPackets[slot].TryReSend(SendImmidiately, connection.GetInitialResendDelay(), false);
                    //        }
                    //    }

                    //} while (curSequence != ackWindowStart);

                    int sendRelate = RelativeSequenceNumber(datagram.Sequence, _lastSequenceOut);
                    if (sendRelate < 0)
                    {
                        if (sendRelate < -_windowSize)
                        {
                            _logger.Trace($"{SignForLogs} very old ack received");
                            return;
                        }

                        //we have sent this message, it's just early
                        if (_sendPendingPackets[datagram.Sequence % START_WINDOW_SIZE].GotAck())
                            //connection.UpdateLatency(sendPendingPackets[ackWindowStart % WINDOW_SIZE].GetDelay());
                            _logger.Trace(
                                $"{SignForLogs} got early ack {datagram}, sendRelate:{sendRelate} relate:{relate}");
                        else
                            _logger.Trace(
                                $"{SignForLogs} got ack {datagram} for packet we're not waiting for {sendRelate} {relate}");
                    }
                    else if (sendRelate > 0)
                    {
                        _logger.Debug($"{SignForLogs} got ack {datagram} for message we have not sent");
                    }
                }
            }
            finally
            {
                datagram.Dispose();
            }
        }


        void AdvanceWindow()
        {
            if (_ordered)
                _recvWithheld[_recvWindowStart % START_WINDOW_SIZE] = null;
            else
                _recvEarlyReceived[_recvWindowStart % START_WINDOW_SIZE] = false;
            _recvWindowStart = (_recvWindowStart + 1) % MAX_SEQUENCE;
        }

        async Task SendAck(Datagram datagram)
        {
            using(Datagram ack = datagram.CreateAck())
                await _connection.SendDatagramAsync(ack, _connection.CancellationToken);
        }

        public override void OnDatagram(Datagram datagram)
        {
            CheckDatagramValid(datagram);

            if (datagram.Type == MessageType.DeliveryAck)
            {
                OnAckReceived(datagram);
                return;
            }

            _ = SendAck(datagram);

            var relate = 0;
            var releaseDatagramBuffer = new List<Datagram>();

            lock (_channelMutex)
            {
                relate = RelativeSequenceNumber(datagram.Sequence, _recvWindowStart);
                if (relate == 0)
                {
                    //right in time
                    releaseDatagramBuffer.Add(datagram);
                    AdvanceWindow();

                    int nextSeqNr = (datagram.Sequence + 1) % MAX_SEQUENCE;

                    if (_ordered)
                        while (_recvWithheld[nextSeqNr % _windowSize] != null)
                        {
                            releaseDatagramBuffer.Add(_recvWithheld[nextSeqNr % _windowSize]);
                            AdvanceWindow();
                            nextSeqNr++;
                        }
                    else
                        while (_recvEarlyReceived[nextSeqNr % _windowSize])
                        {
                            AdvanceWindow();
                            nextSeqNr++;
                        }
                }
            }

            for (var i = 0; i < releaseDatagramBuffer.Count; i++)
                ReleaseDatagram(releaseDatagramBuffer[i]);

            if (relate == 0)
                return;

            if (relate < 0)
            {
                //duplicate
                _logger.Trace($"{SignForLogs} dropped duplicate {datagram}");
                datagram.Dispose();
                return;
            }

            if (relate > _windowSize)
            {
                //too early message
                _logger.Trace($"{SignForLogs} dropped too early {datagram}");
                datagram.Dispose();
                return;
            }

            if (_ordered)
            {
                if (_recvWithheld[datagram.Sequence % START_WINDOW_SIZE] != null)
                {
                    //duplicate
                    _logger.Trace($"{SignForLogs} dropped duplicate {datagram}");
                    datagram.Dispose();
                    return;
                }

                _recvWithheld[datagram.Sequence % START_WINDOW_SIZE] = datagram;
            }
            else
            {
                if (_recvEarlyReceived[datagram.Sequence % START_WINDOW_SIZE])
                {
                    //duplicate
                    _logger.Trace($"{SignForLogs} dropped duplicate {datagram}");
                    datagram.Dispose();
                    return;
                }

                _recvEarlyReceived[datagram.Sequence % START_WINDOW_SIZE] = true;
                ReleaseDatagram(datagram);
            }
        }

        public override void PollEvents()
        {
            lock (_channelMutex) //resending packets
            {
                int ackWindowStart = _ackWindowStart;
                int lastSequenceOut = _lastSequenceOut;

                for (int pendingSeq = ackWindowStart;
                     pendingSeq != lastSequenceOut;
                     pendingSeq = (pendingSeq + 1) % MAX_SEQUENCE)
                {
                    ref PendingPacket pendingPacket = ref _sendPendingPackets[pendingSeq % START_WINDOW_SIZE];
                    int delay = pendingPacket.GetDelay();
                    if (pendingPacket.TryReSend(_connection.GetInitialResendDelay(), true))
                    {
                        _connection.SendDatagramAsync(pendingPacket.Datagram, _connection.CancellationToken);
                        _logger.Debug(
                            $"{SignForLogs} resending {pendingPacket.Datagram} after {delay}ms with num {pendingPacket.ReSendNum}");
                    }
                }
            }
        }

        void TrySendDelayedPackets(int count)
        {
            lock (_channelMutex)
            {
                var counter = 0;
                while (_sendDelayedPackets.Count > 0 && counter++ < count)
                    SendImmediately(_sendDelayedPackets.Dequeue());
            }
        }

        public override async Task SendDatagramAsync(Datagram datagram, CancellationToken cancellationToken)
        {
            CheckDatagramValid(datagram);

            if (!await SendImmediately(datagram).ConfigureAwait(false))
            {
                _logger.Debug(
                    $"{SignForLogs} can't send right now (window is full) {datagram}. Delaying... Queue: {_sendDelayedPackets.Count}");

                var delayedPacket = new DelayedPacket(datagram);
                lock (_channelMutex)
                {
                    _sendDelayedPackets.Enqueue(delayedPacket);
                }

                await delayedPacket.Task.ConfigureAwait(false);
            }
        }


        void SendImmediately(DelayedPacket delayedPacket)
        {
            Datagram datagram = delayedPacket.Datagram;
            datagram.Sequence = GetNextSequenceOut();
            _sendPendingPackets[datagram.Sequence % START_WINDOW_SIZE].Init(datagram);
            _ = _connection.SendDatagramAsync(datagram, _connection.CancellationToken).ContinueWith((t, state) =>
            {
                var delayedPacket_ = (DelayedPacket) state;
                if (t.IsFaulted)
                    delayedPacket_.SetException(t.Exception.GetInnermostException());
                else if (t.IsCompleted)
                    delayedPacket_.SetComplete();
                else
                    delayedPacket_.SetCancelled();
            }, delayedPacket, CancellationToken.None);
        }

        async Task<bool> SendImmediately(Datagram datagram)
        {
            var result = false;
            lock (_channelMutex)
            {
                if (CanSendImmediately())
                {
                    result = true;
                    datagram.Sequence = GetNextSequenceOut();
                    _sendPendingPackets[datagram.Sequence % START_WINDOW_SIZE].Init(datagram);
                }
            }

            if (result)
            {
                await _connection.SendDatagramAsync(datagram, _connection.CancellationToken).ConfigureAwait(false);
                return true;
            }

            return false;
        }

        bool CanSendImmediately()
        {
            int lastSequenceOut = _lastSequenceOut;
            int ackWindowStart = _ackWindowStart;

            int relate = RelativeSequenceNumber(lastSequenceOut, ackWindowStart);
            return relate < _windowSize;
        }
    }
}