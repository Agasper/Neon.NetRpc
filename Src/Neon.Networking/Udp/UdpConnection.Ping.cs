using System;
using System.Threading;
using Neon.Networking.Udp.Channels;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp
{
    public partial class UdpConnection
    {
        int _lastPingSequence;
        DateTime _nextPingSend;
        DateTime? _pingSent;

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
            if (DateTime.UtcNow < _nextPingSend)
                return;
            var sendPingSequence = (ushort) (_lastPingSequence % ChannelBase.MAX_SEQUENCE);
            if (!_pingSent.HasValue)
                _pingSent = DateTime.UtcNow;
            Datagram pingDatagram = Parent.CreateDatagramEmpty(MessageType.Ping, _serviceUnreliableChannel.Descriptor);
            pingDatagram.Sequence = sendPingSequence;
            _serviceUnreliableChannel.SendDatagramAsync(pingDatagram, CancellationToken);
            _nextPingSend = DateTime.UtcNow.AddMilliseconds(Parent.Configuration.KeepAliveInterval);
        }

        void OnPong(Datagram datagram)
        {
            try
            {
                if (!CheckStatusForDatagram(datagram, UdpConnectionStatus.Connected))
                    return;

                int relate = ChannelBase.RelativeSequenceNumber(datagram.Sequence, _lastPingSequence);
                if (relate != 0)
                {
                    _logger.Trace($"#{Id} got wrong pong, relate: {relate}");
                    datagram.Dispose();
                    return;
                }

                if (_pingSent.HasValue)
                    UpdateLatency((int) (DateTime.UtcNow - _pingSent.Value).TotalMilliseconds);

                _pingSent = null;
                Interlocked.Increment(ref _lastPingSequence);
            }
            finally
            {
                datagram.Dispose();
            }
        }

        void UpdateLatency(int latency)
        {
            _latency = latency;
            if (_avgLatency.HasValue)
                _avgLatency = (int) (_avgLatency * 0.7f + latency * 0.3f);
            else
                _avgLatency = latency;

            Statistics.UpdateLatency(latency, _avgLatency.Value);
            _logger.Trace($"#{Id} updated latency {latency}, avg {_avgLatency}");
        }

        void SendPong(Datagram ping)
        {
            Datagram pongDatagram = Parent.CreateDatagramEmpty(MessageType.Pong, _serviceUnreliableChannel.Descriptor);
            pongDatagram.Sequence = ping.Sequence;
            _serviceUnreliableChannel.SendDatagramAsync(pongDatagram, CancellationToken);
        }
    }
}