using System;
using System.Threading;
using Neon.Networking.Udp.Channels;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp
{
	public partial class UdpConnection
	{
        int lastPingSequence = 0;
        DateTime? pingSent;
        DateTime nextPingSend;

        void OnPing(Datagram datagram)
        {
            try
            {
                if (!CheckStatusForDatagram(datagram, UdpConnectionStatus.Connected))
                    return;

                SendPong(datagram);
            }
            finally
            {
                datagram.Dispose();
            }
        }

        void TrySendPing()
        {
            if (DateTime.UtcNow < nextPingSend)
                return;
            ushort sendPingSequence = (ushort)(lastPingSequence % ChannelBase.MAX_SEQUENCE);
            if (!pingSent.HasValue)
                pingSent = DateTime.UtcNow;
            var pingDatagram = peer.CreateDatagramEmpty(MessageType.Ping, serviceUnreliableChannel.Descriptor);
            pingDatagram.Sequence = sendPingSequence;
            serviceUnreliableChannel.SendDatagramAsync(pingDatagram);
            nextPingSend = DateTime.UtcNow.AddMilliseconds(Parent.Configuration.KeepAliveInterval);
        }

        void OnPong(Datagram datagram)
        {
            try
            {
                if (!CheckStatusForDatagram(datagram, UdpConnectionStatus.Connected))
                    return;

                int relate = ChannelBase.RelativeSequenceNumber(datagram.Sequence, lastPingSequence);
                if (relate != 0)
                {
                    logger.Trace($"#{Id} got wrong pong, relate: {relate}");
                    datagram.Dispose();
                    return;
                }
                if (pingSent.HasValue)
                    UpdateLatency((int) (DateTime.UtcNow - pingSent.Value).TotalMilliseconds);

                pingSent = null;
                Interlocked.Increment(ref lastPingSequence);
            }
            finally
            {
                datagram.Dispose();
            }
        }

        void UpdateLatency(int latency)
        {
            this.latency = latency;
            if (avgLatency.HasValue)
                avgLatency = (int)((avgLatency * 0.7f) + (latency * 0.3f));
            else
                avgLatency = latency;

            this.Statistics.UpdateLatency(latency, avgLatency.Value);
            logger.Trace($"#{Id} updated latency {latency}, avg {avgLatency}");
        }

        void SendPong(Datagram ping)
        {
            var pongDatagram = peer.CreateDatagramEmpty(MessageType.Pong, serviceUnreliableChannel.Descriptor);
            pongDatagram.Sequence = ping.Sequence;
            serviceUnreliableChannel.SendDatagramAsync(pongDatagram);
        }


	}
}
