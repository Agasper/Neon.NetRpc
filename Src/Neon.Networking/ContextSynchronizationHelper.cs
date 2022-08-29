using System;
using System.Threading;
using Neon.Logging;

namespace Neon.Networking
{
    public static class ContextSynchronizationHelper
    {
        /// <summary>
        /// Executes a method in the designated context and log any exception occured
        /// </summary>
        /// <param name="context">Designated synchronization context</param>
        /// <param name="mode">Synchronization mode</param>
        /// <param name="logger">A logger to catch exceptions</param>
        /// <param name="nameForLogs">A name for logs of place of exception</param>
        /// <param name="callback">The method</param>
        /// <param name="state">Method state</param>
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

        /// <summary>
        /// Executes a method in the designated context
        /// </summary>
        /// <param name="context">Designated synchronization context</param>
        /// <param name="mode">Synchronization mode</param>
        /// <param name="callback">The method</param>
        /// <param name="state">Method state</param>
        public static void Synchronize(SynchronizationContext context, ContextSynchronizationMode mode,
            SendOrPostCallback callback, object state)
        {
            if (mode == ContextSynchronizationMode.Send)
                context.Send(callback, state);
            else
                context.Post(callback, state);
        }
    }
}
