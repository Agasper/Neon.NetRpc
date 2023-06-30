using System;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp.Channels
{
    abstract class ChannelBase : IChannel
    {
        public const int MAX_SEQUENCE = ushort.MaxValue - 1;

        private protected string SignForLogs =>
            $"#{_connection.Id} channel {Descriptor.DeliveryType}#{Descriptor.Channel}";

        protected readonly object _channelMutex = new object();
        protected readonly IChannelConnection _connection;

        protected readonly ILogger _logger;

        protected int _lastSequenceIn = -1;
        protected int _lastSequenceOut;

        public ChannelBase(ILogManager logManager, ChannelDescriptor descriptor, IChannelConnection connection)
        {
            Descriptor = descriptor;
            _connection = connection;
            _logger = logManager.GetLogger(GetType());
            _logger.Meta.Add("delivery_type", Descriptor.DeliveryType);
            _logger.Meta.Add("channel", Descriptor.Channel);
            _logger.Meta.Add("connection_id", _connection.Id);
        }

        public ChannelDescriptor Descriptor { get; }

        public virtual void Dispose()
        {
        }

        public abstract void OnDatagram(Datagram datagram);
        public abstract Task SendDatagramAsync(Datagram datagram, CancellationToken cancellationToken);

        public virtual void PollEvents()
        {
        }

        protected void CheckDatagramValid(Datagram datagram)
        {
            if (datagram == null) throw new ArgumentNullException(nameof(datagram));
            if (datagram.DeliveryType != Descriptor.DeliveryType)
                throw new ArgumentException($"{nameof(Datagram)} doesn't fit channel by delivery type",
                    nameof(datagram.DeliveryType));
            if (datagram.Channel != Descriptor.Channel)
                throw new ArgumentException($"{nameof(Datagram)} doesn't fit channel by index",
                    nameof(datagram.Channel));
        }

        protected virtual ushort GetNextSequenceOut()
        {
            lock (_channelMutex)
            {
                var newSequence = (ushort) _lastSequenceOut;
                _lastSequenceOut = (ushort) ((_lastSequenceOut + 1) % MAX_SEQUENCE);
                return newSequence;
            }
        }

        protected void ReleaseDatagram(Datagram datagram)
        {
            _logger.Debug($"{SignForLogs} released {datagram}");
            _connection.ReleaseDatagram(this, datagram);
        }

        public override string ToString()
        {
            return
                $"{GetType().Name}[type={Descriptor.DeliveryType},channel={Descriptor.Channel},connectionId={_connection.Id}]]";
        }

        public static int RelativeSequenceNumber(int nr, int expected)
        {
            return (nr - expected + MAX_SEQUENCE + MAX_SEQUENCE / 2) % MAX_SEQUENCE - MAX_SEQUENCE / 2;
        }
    }
}