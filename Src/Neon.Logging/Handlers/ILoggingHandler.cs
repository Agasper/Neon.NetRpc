using System;

namespace Neon.Logging.Handlers
{
    public interface ILoggingHandler
    {
        /// <summary>
        /// Handles the logging event
        /// </summary>
        /// <param name="severity">Log string severity</param>
        /// <param name="message">Logging object</param>
        /// <param name="exception">Exception object</param>
        /// <param name="meta">Final meta information</param>
        /// <param name="logger">Parent logger</param>
        void Write(LogSeverity severity, object message, Exception exception, LoggingMeta meta, ILogger logger);
    }
}