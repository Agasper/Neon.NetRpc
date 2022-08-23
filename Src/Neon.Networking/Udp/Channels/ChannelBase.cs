using System;
using System.Threading.Tasks;
using Neon.Networking.Udp.Messages;
using Neon.Logging;

namespace Neon.Networking.Udp.Channels
{
    abstract class ChannelBase : IChannel
    {
        public const int MAX_SEQUENCE = ushort.MaxValue - 1;

        public ChannelDescriptor Descriptor { get; private set; }

        protected readonly ILogger logger;
        protected readonly IChannelConnection connection;

        protected int lastSequenceIn = -1;
        protected int lastSequenceOut = 0;

        protected readonly object channelMutex = new object();

        public ChannelBase(ILogManager logManager, ChannelDescriptor descriptor, IChannelConnection connection)
        {
            this.Descriptor = descriptor;
            this.connection = connection;
            this.logger = logManager.GetLogger(this.GetType().Name);
            this.logger.Meta.Add("delivery_type", this.Descriptor.DeliveryType);
            this.logger.Meta.Add("channel", this.Descriptor.Channel);
            this.logger.Meta.Add("connection_id", this.connection.Id);
        }

        public virtual void Dispose()
        {

        }

        public abstract void OnDatagram(Datagram datagram);
        public abstract Task SendDatagramAsync(Datagram datagram);

        public virtual void PollEvents()
        {

        }

        protected void CheckDatagramValid(Datagram datagram)
        {
            if (datagram == null) throw new ArgumentNullException(nameof(datagram));
            if (datagram.DeliveryType != this.Descriptor.DeliveryType)
                throw new ArgumentException($"{nameof(Datagram)} doesn't fit channel by delivery type", nameof(datagram.DeliveryType));
            if (datagram.Channel != this.Descriptor.Channel)
                throw new ArgumentException($"{nameof(Datagram)} doesn't fit channel by index", nameof(datagram.Channel));
        }

        protected virtual ushort GetNextSequenceOut()
        {
            lock (channelMutex)
            {
                ushort newSequence = (ushort) lastSequenceOut;
                lastSequenceOut = (ushort) ((lastSequenceOut + 1) % MAX_SEQUENCE);
                return newSequence;
            }
        }

        protected void ReleaseDatagram(Datagram datagram)
        {
            logger.Debug($"{SignForLogs} released {datagram}");
            connection.ReleaseDatagram(this, datagram);
        }

        public override string ToString()
        {
            return $"{this.GetType().Name}[type={this.Descriptor.DeliveryType},channel={this.Descriptor.Channel},connectionId={connection.Id}]]";
        }

        private protected string SignForLogs => $"#{connection.Id} channel {Descriptor.DeliveryType}#{Descriptor.Channel}";

        public static int RelativeSequenceNumber(int nr, int expected)
        {
            return (nr - expected + MAX_SEQUENCE + (MAX_SEQUENCE / 2)) % MAX_SEQUENCE - (MAX_SEQUENCE / 2);
        }
    }
}
