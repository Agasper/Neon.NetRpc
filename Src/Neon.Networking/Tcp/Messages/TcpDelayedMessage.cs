using System;
using System.Threading.Tasks;

namespace Neon.Networking.Tcp.Messages
{
    class TcpDelayedMessage //for latency simulation
    {
        public TcpMessage Message { get; }
        public DateTime ReleaseTimestamp { get; }
        TaskCompletionSource<TcpDelayedMessage> _taskCompletionSource;

        public TcpDelayedMessage(TcpMessage message, DateTime releaseTimestamp)
        {
            Message = message;
            ReleaseTimestamp = releaseTimestamp;
        }

        public Task GetTask()
        {
            if (_taskCompletionSource == null)
                _taskCompletionSource = new TaskCompletionSource<TcpDelayedMessage>();
            return _taskCompletionSource.Task;
        }

        public void Complete(Task task)
        {
            if (_taskCompletionSource == null)
                return;
            if (task.IsCanceled)
                _taskCompletionSource.TrySetCanceled();
            if (task.IsFaulted)
                _taskCompletionSource.TrySetException(task.Exception ?? new Exception("Unknown exception"));
            if (task.IsCompleted && !task.IsFaulted)
                _taskCompletionSource.TrySetResult(this);
        }
    }
}