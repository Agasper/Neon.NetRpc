using System.Collections.Generic;
using Neon.Logging.Handlers;

namespace Neon.Logging
{
    public interface ILogger
    {
        /// <summary>
        /// Logger severity level
        /// </summary>
        LogSeverity Severity { get; set; }
        /// <summary>
        /// Logger name
        /// </summary>
        string Name { get; set;  }
        /// <summary>
        /// Logger meta information, will be merged with LogManager meta and record meta 
        /// </summary>
        LoggingMeta Meta { get; }
        /// <summary>
        /// Logger extra handlers
        /// </summary>
        LoggingHandlers Handlers { get; }

        /// <summary>Writes the specified object with TRACE logging severity</summary>
        /// <param name="message">The value to write.</param>
        void Trace(object message);
        /// <summary>Writes the specified object and its meta with TRACE logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="meta">Meta information of the record</param>
        void Trace(object message, LoggingMeta meta);
        /// <summary>Writes the specified object with DEBUG logging severity</summary>
        /// <param name="message">The value to write.</param>
        void Debug(object message);
        /// <summary>Writes the specified object and its meta with DEBUG logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="meta">Meta information of the record</param>
        void Debug(object message, LoggingMeta meta);
        /// <summary>Writes the specified object with INFO logging severity</summary>
        /// <param name="message">The value to write.</param>
        void Info(object message);
        /// <summary>Writes the specified object and its meta with INFO logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="meta">Meta information of the record</param>
        void Info(object message, LoggingMeta meta);
        /// <summary>Writes the specified object with WARNING logging severity</summary>
        /// <param name="message">The value to write.</param>
        void Warn(object message);
        /// <summary>Writes the specified object and its meta with WARNING logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="meta">Meta information of the record</param>
        void Warn(object message, LoggingMeta meta);
        /// <summary>Writes the specified object with ERROR logging severity</summary>
        /// <param name="message">The value to write.</param>
        void Error(object message);
        /// <summary>Writes the specified object and its meta with ERROR logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="meta">Meta information of the record</param>
        void Error(object message, LoggingMeta meta);
        /// <summary>Writes the specified object with CRITICAL logging severity</summary>
        /// <param name="message">The value to write.</param>
        void Critical(object message);
        /// <summary>Writes the specified object and its meta with CRITICAL logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="meta">Meta information of the record</param>
        void Critical(object message, LoggingMeta meta);

        /// <summary>Writes the specified object</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="severity">Severity of the record</param>
        void Write(LogSeverity severity, object message);
        /// <summary>Writes the specified object</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="severity">Severity of the record</param>
        /// <param name="meta">Meta information of the record</param>
        void Write(LogSeverity severity, object message, LoggingMeta meta);

        /// <summary>
        /// Check whether severity level is available
        /// </summary>
        /// <param name="level">Severity level</param>
        /// <returns>True if enabled, False if not</returns>
        bool IsLevelEnabled(LogSeverity level);
    }
}
