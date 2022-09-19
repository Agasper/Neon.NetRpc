using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neon.Networking.Udp.Messages;
using Neon.Logging;
using Neon.Util;

namespace Neon.Networking.Udp.Channels
{
    class ReliableChannel : ChannelBase
    {
        const int START_WINDOW_SIZE = 64;

        int recvWindowStart;
        bool[] recvEarlyReceived;
        Datagram[] recvWithheld;

        int ackWindowStart;
        int windowSize;
        PendingPacket[] sendPendingPackets;
        Queue<DelayedPacket> sendDelayedPackets;

        bool ordered;
        bool disposed;

        public ReliableChannel(ILogManager logManager,
            ChannelDescriptor descriptor, IChannelConnection connection, bool ordered)
                : base(logManager, descriptor, connection)
        {
            this.ordered = ordered;
            this.recvWindowStart = 0;
            this.ackWindowStart = 0;
            this.windowSize = START_WINDOW_SIZE;
            if (ordered)
                this.recvWithheld = new Datagram[START_WINDOW_SIZE];
            else
                this.recvEarlyReceived = new bool[START_WINDOW_SIZE];
            this.sendPendingPackets = new PendingPacket[START_WINDOW_SIZE];
            this.sendDelayedPackets = new Queue<DelayedPacket>();
        }

        public override void Dispose()
        {
            if (!disposed)
                return;
            disposed = true;
            lock (channelMutex)
            {
                if (recvWithheld != null)
                {
                    for (int i = 0; i < recvWithheld.Length; i++)
                    {
                        var record = recvWithheld[i];
                        if (record != null)
                            record.Dispose();
                        recvWithheld[i] = null;
                    }
                }

                for (int i = 0; i < sendPendingPackets.Length; i++)
                {
                    sendPendingPackets[i].Clear();
                }

                foreach (var delayedPacket in sendDelayedPackets)
                {
                    delayedPacket.Dispose();
                }
            }
        }


        void OnAckReceived(Datagram datagram)
        {
            try
            {
                lock (channelMutex)
                {
                    int relate = RelativeSequenceNumber(datagram.Sequence, ackWindowStart);
                    if (relate < 0)
                    {
                        logger.Trace($"{SignForLogs} got duplicate/late ack");
                        return; //late/duplicate ack
                    }

                    if (relate == 0)
                    {
                        logger.Trace($"{SignForLogs} got ack {datagram} just in time, clearing pending datagrams...");
                        //connection.UpdateLatency(sendPendingPackets[ackWindowStart % WINDOW_SIZE].GetDelay());

                        int delayedPacketsCounter = 0;

                        sendPendingPackets[ackWindowStart % windowSize].Clear();
                        ackWindowStart = (ackWindowStart + 1) % MAX_SEQUENCE;
                        delayedPacketsCounter++;

                        while (sendPendingPackets[ackWindowStart % windowSize].AckReceived)
                        {
                            logger.Trace(
                                $"{SignForLogs} clearing early pending {sendPendingPackets[ackWindowStart % windowSize].Datagram}");
                            sendPendingPackets[ackWindowStart % windowSize].Clear();
                            ackWindowStart = (ackWindowStart + 1) % MAX_SEQUENCE;
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

                    int sendRelate = RelativeSequenceNumber(datagram.Sequence, lastSequenceOut);
                    if (sendRelate < 0)
                    {
                        if (sendRelate < -windowSize)
                        {
                            logger.Trace($"{SignForLogs} very old ack received");
                            return;
                        }

                        //we have sent this message, it's just early
                        if (sendPendingPackets[datagram.Sequence % START_WINDOW_SIZE].GotAck())
                        {
                            //connection.UpdateLatency(sendPendingPackets[ackWindowStart % WINDOW_SIZE].GetDelay());
                            logger.Trace($"{SignForLogs} got early ack {datagram}, sendRelate:{sendRelate} relate:{relate}");
                        }
                        else
                            logger.Trace($"{SignForLogs} got ack {datagram} for packet we're not waiting for {sendRelate} {relate}");
                    }
                    else if (sendRelate > 0)
                    {
                        logger.Debug($"{SignForLogs} got ack {datagram} for message we have not sent");
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
            if (ordered)
                recvWithheld[recvWindowStart % START_WINDOW_SIZE] = null;
            else
                recvEarlyReceived[recvWindowStart % START_WINDOW_SIZE] = false;
            recvWindowStart = (recvWindowStart + 1) % MAX_SEQUENCE;
        }

        public override void OnDatagram(Datagram datagram)
        {
            CheckDatagramValid(datagram);

            if (datagram.Type == MessageType.DeliveryAck)
            {
                OnAckReceived(datagram);
                return;
            }

            _ = connection.SendDatagramAsync(datagram.CreateAck());

            int relate = 0;
            List<Datagram> releaseDatagramBuffer = new List<Datagram>();

            lock (channelMutex)
            {
                relate = RelativeSequenceNumber(datagram.Sequence, recvWindowStart);
                if (relate == 0)
                {
                    //right in time
                    releaseDatagramBuffer.Add(datagram);
                    AdvanceWindow();

                    int nextSeqNr = (datagram.Sequence + 1) % MAX_SEQUENCE;

                    if (ordered)
                    {
                        while (recvWithheld[nextSeqNr % windowSize] != null)
                        {
                            releaseDatagramBuffer.Add(recvWithheld[nextSeqNr % windowSize]);
                            AdvanceWindow();
                            nextSeqNr++;
                        }
                    }
                    else
                    {
                        while (recvEarlyReceived[nextSeqNr % windowSize])
                        {
                            AdvanceWindow();
                            nextSeqNr++;
                        }
                    }
                }
            }

            for (int i = 0; i < releaseDatagramBuffer.Count; i++)
                ReleaseDatagram(releaseDatagramBuffer[i]);

            if (relate == 0)
                return;

            if (relate < 0)
            {
                //duplicate
                logger.Trace($"{SignForLogs} dropped duplicate {datagram}");
                datagram.Dispose();
                return;
            }

            if (relate > windowSize)
            {
                //too early message
                logger.Trace($"{SignForLogs} dropped too early {datagram}");
                datagram.Dispose();
                return;
            }

            if (ordered)
            {
                if (recvWithheld[datagram.Sequence % START_WINDOW_SIZE] != null)
                {
                    //duplicate
                    logger.Trace($"{SignForLogs} dropped duplicate {datagram}");
                    datagram.Dispose();
                    return;
                }

                recvWithheld[datagram.Sequence % START_WINDOW_SIZE] = datagram;
            }
            else
            {
                if (recvEarlyReceived[datagram.Sequence % START_WINDOW_SIZE])
                {
                    //duplicate
                    logger.Trace($"{SignForLogs} dropped duplicate {datagram}");
                    datagram.Dispose();
                    return;
                }

                recvEarlyReceived[datagram.Sequence % START_WINDOW_SIZE] = true;
                ReleaseDatagram(datagram);
            }
        }

        public override void PollEvents()
        {
            lock (channelMutex) //resending packets
            {
                int ackWindowStart = this.ackWindowStart;
                int lastSequenceOut = this.lastSequenceOut;

                for (int pendingSeq = ackWindowStart; pendingSeq != lastSequenceOut; pendingSeq = (pendingSeq + 1) % MAX_SEQUENCE)
                {
                    ref PendingPacket pendingPacket = ref sendPendingPackets[pendingSeq % START_WINDOW_SIZE];
                    var delay = pendingPacket.GetDelay();
                    if (pendingPacket.TryReSend(connection.GetInitialResendDelay(), true))
                    {
                        connection.SendDatagramAsync(pendingPacket.Datagram);
                        logger.Debug($"{SignForLogs} resending {pendingPacket.Datagram} after {delay}ms with num {pendingPacket.ReSendNum}");
                    }
                }
            }
        }

        void TrySendDelayedPackets(int count)
        {
            lock (channelMutex)
            {
                int counter = 0;
                while (sendDelayedPackets.Count > 0 && counter++ < count)
                {
                    SendImmediately(sendDelayedPackets.Dequeue());
                }
            }
        }

        public override async Task SendDatagramAsync(Datagram datagram)
        {
            CheckDatagramValid(datagram);

            if (!await SendImmediately(datagram).ConfigureAwait(false))
            {
                logger.Debug(
                    $"{SignForLogs} can't send right now (window is full) {datagram}. Delaying... Queue: {sendDelayedPackets.Count}");

                DelayedPacket delayedPacket = new DelayedPacket(datagram);
                lock (channelMutex)
                {
                    sendDelayedPackets.Enqueue(delayedPacket);
                }

                await delayedPacket.Task.ConfigureAwait(false);
            }
        }


        void SendImmediately(DelayedPacket delayedPacket)
        {
            var datagram = delayedPacket.Datagram;
            datagram.Sequence = GetNextSequenceOut();
            sendPendingPackets[datagram.Sequence % START_WINDOW_SIZE].Init(datagram);
            _ = connection.SendDatagramAsync(datagram).ContinueWith((t, state) =>
            {
                DelayedPacket delayedPacket_ = (DelayedPacket)state;
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
            bool result = false;
            lock (channelMutex)
            {
                if (CanSendImmediately())
                {
                    result = true;
                    datagram.Sequence = GetNextSequenceOut();
                    sendPendingPackets[datagram.Sequence % START_WINDOW_SIZE].Init(datagram);
                }
            }

            if (result)
            {
                await connection.SendDatagramAsync(datagram).ConfigureAwait(false);
                return true;
            }

            return false;
        }

        bool CanSendImmediately()
        {
            int lastSequenceOut = this.lastSequenceOut;
            int ackWindowStart = this.ackWindowStart;

            int relate = RelativeSequenceNumber(lastSequenceOut, ackWindowStart);
            return relate < windowSize;
        }
    }
}
