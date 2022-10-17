using System;
using System.Threading.Tasks;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp.Channels
{
    struct DelayedPacket : IDisposable
    {
        public Datagram Datagram { get; }
        public Task Task => tcs.Task;

        readonly TaskCompletionSource<object> tcs;

        public DelayedPacket(Datagram datagram)
        {
            this.Datagram = datagram;
            this.tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void Dispose()
        {
            Datagram?.Dispose();
        }

        public void SetComplete()
        {
            tcs?.TrySetResult(null);
        }
        
        public void SetCancelled()
        {
            tcs?.TrySetCanceled();
        }

        public void SetException(Exception exception)
        {
            tcs?.TrySetException(exception);
        }
    }
}