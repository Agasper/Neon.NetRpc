using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp
{
    struct SendDatagramAsyncResult
    {
        public UdpConnection Connection { get;  }
        public Datagram Datagram { get;  }
        public Task<SocketError> Task => tcs.Task;

        TaskCompletionSource<SocketError> tcs;

        public SendDatagramAsyncResult(UdpConnection connection, Datagram datagram)
        {
            this.Datagram = datagram;
            this.Connection = connection;
            this.tcs = new TaskCompletionSource<SocketError>();
        }

        public void SetComplete(SocketError socketError)
        {
            tcs?.TrySetResult(socketError);
        }

        public void SetException(Exception exception)
        {
            tcs?.TrySetException(exception);
        }
    }
}
