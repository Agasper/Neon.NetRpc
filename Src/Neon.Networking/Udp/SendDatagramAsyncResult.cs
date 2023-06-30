using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp
{
    struct SendDatagramAsyncResult
    {
        public UdpConnection Connection { get; }
        public Datagram Datagram { get; }
        public Task<SocketError> Task => _tcs.Task;

        readonly TaskCompletionSource<SocketError> _tcs;

        public SendDatagramAsyncResult(UdpConnection connection, Datagram datagram)
        {
            Datagram = datagram;
            Connection = connection;
            _tcs = new TaskCompletionSource<SocketError>();
        }

        public void SetComplete(SocketError socketError)
        {
            _tcs?.TrySetResult(socketError);
        }

        public void SetException(Exception exception)
        {
            _tcs?.TrySetException(exception);
        }
    }
}