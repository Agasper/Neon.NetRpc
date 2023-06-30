using System;

namespace Neon.Logging
{
    public class Logger : ILogger
    {
        readonly LogManager _parent;

        internal Logger(string name, LogManager parent)
        {
            Name = name;
            _parent = parent;
            Meta = new LoggingMeta();
        }

        /// <summary>
        ///     Logger name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Logger meta information, will be merged with LogManager meta and record meta
        /// </summary>
        public LoggingMeta Meta { get; }

        /// <summary>Writes the specified object with TRACE logging severity</summary>
        /// <param name="message">The value to write.</param>
        public void Trace(object message)
        {
            Write(LogSeverity.TRACE, message);
        }

        /// <summary>Writes the specified object and its meta with TRACE logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="meta">Meta information of the record</param>
        public void Trace(object message, LoggingMeta meta)
        {
            Write(LogSeverity.TRACE, message, null, meta);
        }

        /// <summary>Writes the specified object with DEBUG logging severity</summary>
        /// <param name="message">The value to write.</param>
        public void Debug(object message)
        {
            Write(LogSeverity.DEBUG, message);
        }

        /// <summary>Writes the specified object and its meta with DEBUG logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="meta">Meta information of the record</param>
        public void Debug(object message, LoggingMeta meta)
        {
            Write(LogSeverity.DEBUG, message, null, meta);
        }

        /// <summary>Writes the specified object with INFO logging severity</summary>
        /// <param name="message">The value to write.</param>
        public void Info(object message)
        {
            Write(LogSeverity.INFO, message);
        }

        /// <summary>Writes the specified object and its meta with INFO logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="meta">Meta information of the record</param>
        public void Info(object message, LoggingMeta meta)
        {
            Write(LogSeverity.INFO, message, null, meta);
        }

        /// <summary>Writes the specified object with WARNING logging severity</summary>
        /// <param name="message">The value to write.</param>
        public void Warn(object message)
        {
            Write(LogSeverity.WARNING, message);
        }

        /// <summary>Writes the specified object and its meta with WARNING logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="meta">Meta information of the record</param>
        public void Warn(object message, LoggingMeta meta)
        {
            Write(LogSeverity.WARNING, message, null, meta);
        }

        /// <summary>Writes the specified object with ERROR logging severity</summary>
        /// <param name="message">The value to write.</param>
        public void Error(object message)
        {
            Write(LogSeverity.ERROR, message);
        }

        /// <summary>Writes the specified object with ERROR logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="exception">Exception object</param>
        public void Error(object message, Exception exception)
        {
            Write(LogSeverity.ERROR, message, exception);
        }

        /// <summary>Writes the specified object and its meta with ERROR logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="meta">Meta information of the record</param>
        public void Error(object message, LoggingMeta meta)
        {
            Write(LogSeverity.ERROR, message, null, meta);
        }

        /// <summary>Writes the specified object with ERROR logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="exception">Exception object</param>
        /// <param name="meta">Meta information of the record</param>
        public void Error(object message, Exception exception, LoggingMeta meta)
        {
            Write(LogSeverity.ERROR, message, exception, meta);
        }

        /// <summary>Writes the specified object with CRITICAL logging severity</summary>
        /// <param name="message">The value to write.</param>
        public void Critical(object message)
        {
            Write(LogSeverity.CRITICAL, message);
        }

        /// <summary>Writes the specified object with CRITICAL logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="exception">Exception object</param>
        public void Critical(object message, Exception exception)
        {
            Write(LogSeverity.CRITICAL, message, exception);
        }

        /// <summary>Writes the specified object and its meta with CRITICAL logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="meta">Meta information of the record</param>
        public void Critical(object message, LoggingMeta meta)
        {
            Write(LogSeverity.CRITICAL, message, null, meta);
        }

        /// <summary>Writes the specified object with CRITICAL logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="exception">Exception object</param>
        /// <param name="meta">Meta information of the record</param>
        public void Critical(object message, Exception exception, LoggingMeta meta)
        {
            Write(LogSeverity.CRITICAL, message, exception, meta);
        }

        /// <summary>Writes the specified object</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="severity">Severity of the record</param>
        public void Write(LogSeverity severity, object message)
        {
            Write(severity, message, null, LoggingMeta.Empty);
        }

        /// <summary>Writes the specified object</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="exception">Exception object</param>
        /// <param name="severity">Severity of the record</param>
        public void Write(LogSeverity severity, object message, Exception exception)
        {
            Write(severity, message, exception, LoggingMeta.Empty);
        }

        /// <summary>Writes the specified object</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="meta">Meta information of the record</param>
        /// <param name="severity">Severity of the record</param>
        public void Write(LogSeverity severity, object message, LoggingMeta meta)
        {
            Write(severity, message, null, Meta);
        }

        /// <summary>
        ///     Check is the record with specified severity will be filtered
        /// </summary>
        /// <param name="severity">Severity</param>
        /// <returns></returns>
        public bool IsFiltered(LogSeverity severity)
        {
            return _parent.IsFiltered(this, severity);
        }

        /// <summary>Writes the specified object</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="severity">Severity of the record</param>
        /// <param name="exception">Exception object</param>
        /// <param name="meta">Meta information of the record</param>
        public void Write(LogSeverity severity, object message, Exception exception, LoggingMeta meta)
        {
            if (meta == null) throw new ArgumentNullException(nameof(meta));
            if (_parent.IsFiltered(this, severity))
                return;

            LoggingMeta mergedMeta = LoggingMeta.Merge(_parent.Meta, Meta, meta);

            for (var i = 0; i < _parent.Handlers.Count; i++)
                _parent.Handlers[i].Write(severity, message, exception, mergedMeta, this);
        }
    }
}