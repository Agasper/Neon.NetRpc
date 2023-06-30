using System;
using System.Threading.Tasks;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp.Channels
{
    struct DelayedPacket : IDisposable
    {
        public Datagram Datagram { get; }
        public Task Task => _tcs.Task;

        readonly TaskCompletionSource<object> _tcs;

        public DelayedPacket(Datagram datagram)
        {
            Datagram = datagram;
            _tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void Dispose()
        {
            Datagram?.Dispose();
        }

        public void SetComplete()
        {
            _tcs?.TrySetResult(null);
        }

        public void SetCancelled()
        {
            _tcs?.TrySetCanceled();
        }

        public void SetException(Exception exception)
        {
            _tcs?.TrySetException(exception);
        }
    }
}