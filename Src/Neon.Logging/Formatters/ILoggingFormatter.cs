using System;

namespace Neon.Logging.Formatters
{
    public interface ILoggingFormatter
    {
        /// <summary>
        /// Converts any incoming object with meta to the log string
        /// </summary>
        /// <param name="severity">Final row severity</param>
        /// <param name="message">Object to format</param>
        /// <param name="exception">Exception object</param>
        /// <param name="meta">Final meta information</param>
        /// <param name="logger">Parent logger</param>
        /// <returns>Log string</returns>
        string Format(LogSeverity severity, object message, Exception exception, LoggingMeta meta, ILogger logger);
    }
}