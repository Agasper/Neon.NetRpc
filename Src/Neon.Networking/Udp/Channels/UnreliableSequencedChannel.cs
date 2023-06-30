using System;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp.Channels
{
    class UnreliableSequencedChannel : ChannelBase
    {
        public UnreliableSequencedChannel(ILogManager logManager,
            ChannelDescriptor descriptor, IChannelConnection connection)
            : base(logManager, descriptor, connection)
        {
        }


        public override async Task SendDatagramAsync(Datagram datagram, CancellationToken cancellationToken)
        {
            if (datagram == null) throw new ArgumentNullException(nameof(datagram));
            try
            {
                CheckDatagramValid(datagram);
                datagram.Sequence = GetNextSequenceOut();
                await _connection.SendDatagramAsync(datagram, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                datagram.Dispose();
            }
        }

        public override void OnDatagram(Datagram datagram)
        {
            CheckDatagramValid(datagram);

            int relate = RelativeSequenceNumber(datagram.Sequence, _lastSequenceIn + 1);
            if (relate < 0)
            {
                _logger.Debug($"{SignForLogs} dropping old {datagram}");
                datagram.Dispose();
                return; //drop old
            }

            _lastSequenceIn = datagram.Sequence;
            ReleaseDatagram(datagram);
        }
    }
}