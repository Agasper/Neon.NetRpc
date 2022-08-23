using System;
using System.Threading.Tasks;

namespace Neon.Networking.Tcp.Messages
{
    class TcpDelayedMessage //for latency simulation
    {
        public TcpMessage Message { get; }
        public DateTime ReleaseTimestamp { get; }

        TaskCompletionSource<TcpDelayedMessage> taskCompletionSource;

        public TcpDelayedMessage(TcpMessage message, DateTime releaseTimestamp)
        {
            this.Message = message;
            this.ReleaseTimestamp = releaseTimestamp;
        }

        public Task GetTask()
        {
            if (taskCompletionSource == null)
                taskCompletionSource = new TaskCompletionSource<TcpDelayedMessage>();
            return taskCompletionSource.Task;
        }

        public void Complete(Task task)
        {
            if (taskCompletionSource == null)
                return;
            if (task.IsCanceled)
                taskCompletionSource.TrySetCanceled();
            if (task.IsFaulted)
                taskCompletionSource.TrySetException(task.Exception ?? new Exception("Unknown exception"));
            if (task.IsCompleted && !task.IsFaulted)
                taskCompletionSource.TrySetResult(this);
        }
    }
}