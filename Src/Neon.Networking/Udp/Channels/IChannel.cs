using System;
using System.Threading;
using System.Threading.Tasks;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp.Channels
{
    interface IChannel : IDisposable
    {
        ChannelDescriptor Descriptor { get; }

        void OnDatagram(Datagram datagram);
        Task SendDatagramAsync(Datagram datagram, CancellationToken cancellationToken);

        void PollEvents();
    }
}