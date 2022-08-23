using System;
using System.Threading.Tasks;
using Neon.Networking.Udp.Messages;
using Neon.Logging;

namespace Neon.Networking.Udp.Channels
{
    class UnreliableSequencedChannel : ChannelBase
    {
        public UnreliableSequencedChannel(ILogManager logManager,
            ChannelDescriptor descriptor, IChannelConnection connection)
            : base(logManager, descriptor, connection)
        {

        }


        public override async Task SendDatagramAsync(Datagram datagram)
        {
            if (datagram == null) throw new ArgumentNullException(nameof(datagram));
            try
            {
                CheckDatagramValid(datagram);
                datagram.Sequence = GetNextSequenceOut();
                await connection.SendDatagramAsync(datagram).ConfigureAwait(false);
            }
            finally
            {
                datagram.Dispose();
            }
        }

        public override void OnDatagram(Datagram datagram)
        {
            CheckDatagramValid(datagram);

            int relate = RelativeSequenceNumber(datagram.Sequence, lastSequenceIn + 1);
            if (relate < 0)
            {
                logger.Debug($"{SignForLogs} dropping old {datagram}");
                datagram.Dispose();
                return; //drop old
            }

            lastSequenceIn = datagram.Sequence;
            ReleaseDatagram(datagram);
        }
    }
}
