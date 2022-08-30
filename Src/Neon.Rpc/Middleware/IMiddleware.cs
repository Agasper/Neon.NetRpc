using System.Threading;
using System.Threading.Tasks;
using Neon.Networking.Messages;

namespace Middleware
{
    interface IMiddleware
    {
        Task Start(CancellationToken cancellationToken);
        
        void MiddlewareMessage(RawMessage message);
        
        RawMessage ReceiveMessage(RawMessage message);
        RawMessage SendMessage(RawMessage message);
    }
}