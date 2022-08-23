using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neon.Util
{
    public static class TaskExtensions
    {
        static OperationCanceledException GetOperationCancelledException()
        {
            return new OperationCanceledException("Operation cancelled");
        }
        
        static TimeoutException GetTimeoutException()
        {
            return new TimeoutException("Operation timed out");
        }
        
        public static async Task TimeoutAfter(this Task task, int millisecondsTimeout,
            CancellationToken cancellationToken = default)
        {
            if (task.IsCompleted)
            {
                return;
            }

            using (CancellationTokenSource cts = new CancellationTokenSource(millisecondsTimeout))
            {
                using (CancellationTokenSource childCts =
                       CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken))
                {

                    TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                    _ = task.ContinueWith(
                        (t, state) => { MarshalTaskResults(t, state as TaskCompletionSource<object>); }, tcs,
                        TaskContinuationOptions.ExecuteSynchronously);

                    using (childCts.Token.Register((state) =>
                           {
                               Exception exception = null;
                               if (((CancellationToken) state).IsCancellationRequested)
                                   exception = GetTimeoutException();
                               else
                                   exception = GetOperationCancelledException();
                               tcs.TrySetException(exception);
                           }, cts.Token))
                    {
                        await tcs.Task.ConfigureAwait(false);
                    }
                }
            }
        }

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, int millisecondsTimeout, CancellationToken cancellationToken = default)
        {
            if (task.IsCompleted)
            {
                return await task.ConfigureAwait(false);
            }

            using (CancellationTokenSource cts = new CancellationTokenSource(millisecondsTimeout))
            {
                using (CancellationTokenSource childCts =
                       CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken))
                {

                    TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();

                    _ = task.ContinueWith(
                        (t, state) => { MarshalTaskResults(t, state as TaskCompletionSource<TResult>); }, tcs,
                        TaskContinuationOptions.ExecuteSynchronously);

                    using (childCts.Token.Register((state) =>
                           {
                               Exception exception = null;
                               if (((CancellationToken) state).IsCancellationRequested)
                                   exception = GetTimeoutException();
                               else
                                   exception = GetOperationCancelledException();
                               tcs.TrySetException(exception);
                           }, cts.Token))
                    {
                        return await tcs.Task.ConfigureAwait(false);
                    }
                }
            }
        }

        internal static void MarshalTaskResults<TResult>(
            Task source, TaskCompletionSource<TResult> proxy)
        {
            switch (source.Status)
            {
                case TaskStatus.Faulted:
                    if (source.Exception.InnerExceptions.Count == 1)
                        proxy.TrySetException(source.Exception.InnerExceptions.First());
                    else
                        proxy.TrySetException(source.Exception);
                    break;
                case TaskStatus.Canceled:
                    proxy.TrySetCanceled();
                    break;
                case TaskStatus.RanToCompletion:
                    Task<TResult> castedSource = source as Task<TResult>;
                    proxy.TrySetResult(
                        castedSource == null ? default(TResult) : // source is a Task
                            castedSource.Result); // source is a Task<TResult>
                    break;
                default:
                    throw new InvalidOperationException("Task has invalid status for marshaling: " + source.Status.ToString());
            }
        }
    }
}
