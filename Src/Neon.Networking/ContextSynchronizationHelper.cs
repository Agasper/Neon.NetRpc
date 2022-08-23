using System;
using System.Threading;
using Neon.Logging;

namespace Neon.Networking
{
    public static class ContextSynchronizationHelper
    {
        public static void SynchronizeSafe(SynchronizationContext context, ContextSynchronizationMode mode, ILogger logger, string nameForLogs, SendOrPostCallback callback, object state)
        {
            try
            {
                if (mode == ContextSynchronizationMode.Send)
                    context.Send(callback, state);
                else
                    context.Post(callback, state);
            }
            catch (Exception ex)
            {
                logger.Error($"Unhandled exception on context synchronization in `{nameForLogs}`: {ex}");
            }
        }
    }
}
