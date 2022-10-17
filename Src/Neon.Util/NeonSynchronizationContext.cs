using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neon.Util
{
    /// <summary>
    /// A placeholder for a synchronization context.
    /// </summary>
    public class NeonSynchronizationContext : SynchronizationContext
    {
        Task task = Task.CompletedTask;
        object mutex = new object();
        
        public override void Post(SendOrPostCallback d, object state)
        {
            lock (mutex)
            {
                task = task.ContinueWith(
                    (task, args_) =>
                    {
                        Tuple<SendOrPostCallback, object> args__ = (Tuple<SendOrPostCallback, object>) args_;
                        args__.Item1(args__.Item2);
                    }, new Tuple<SendOrPostCallback, object>(d, state), TaskContinuationOptions.RunContinuationsAsynchronously);
            }
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            d(state);
        }
    }
}