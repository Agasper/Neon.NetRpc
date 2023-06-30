using System;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp.Channels
{
    class UnreliableChannel : ChannelBase
    {
        public UnreliableChannel(ILogManager logManager,
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
                // datagram.Sequence = GetNextSequenceOut();
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
            ReleaseDatagram(datagram);
        }
    }
}