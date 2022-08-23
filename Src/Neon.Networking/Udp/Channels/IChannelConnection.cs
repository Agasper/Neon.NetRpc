using System.Net.Sockets;
using System.Threading.Tasks;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp.Channels
{
    interface IChannelConnection
    {
        long Id { get; }
        int GetInitialResendDelay();
        void ReleaseDatagram(IChannel channel, Datagram datagram);
        Task SendDatagramAsync(Datagram datagram);
    }
}
