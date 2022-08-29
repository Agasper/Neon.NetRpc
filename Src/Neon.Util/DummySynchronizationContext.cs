using System.Threading;

namespace Neon.Util
{
    /// <summary>
    /// A placeholder for a synchronization context.
    /// </summary>
    public class DummySynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object state)
        {
            d(state);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            d(state);
        }
    }
}