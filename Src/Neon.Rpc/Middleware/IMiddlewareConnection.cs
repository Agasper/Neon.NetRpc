using System.Threading.Tasks;
using Neon.Networking.Messages;
using Neon.Networking.Udp.Messages;

namespace Middleware
{
    public interface IMiddlewareConnection
    {
        long Id { get; }
        bool Connected { get; }
        bool IsClientConnection { get; }

        RawMessage CreateMessage();
        RawMessage CreateMessage(int length);
        
        void Close();
        Task SendMessageWithMiddlewaresAsync(RawMessage rawMessage, DeliveryType deliveryType, int channel);
    }
}