using System;

namespace Neon.Logging
{
    public class Logger : ILogger
    {
        /// <summary>
        /// Logger severity level
        /// </summary>
        public LogSeverity Severity { get; set; }
        /// <summary>
        /// Logger name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Logger meta information, will be merged with LogManager meta and record meta 
        /// </summary>
        public LoggingMeta Meta => meta;
        /// <summary>
        /// Logger extra handlers
        /// </summary>
        public LoggingHandlers Handlers => handlers;

        readonly LogManager parent;
        readonly LoggingHandlers handlers;
        readonly LoggingMeta meta;

        internal Logger(string name, LogManager parent)
        {
            this.Name = name;
            this.Severity = LogSeverity.TRACE;
            this.parent = parent;
            this.meta = new LoggingMeta();
            this.handlers = new LoggingHandlers();
        }

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
            Write(LogSeverity.TRACE, message, meta);
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
            Write(LogSeverity.DEBUG, message, meta);
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
            Write(LogSeverity.INFO, message, meta);
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
            Write(LogSeverity.WARNING, message, meta);
        }

        /// <summary>Writes the specified object with ERROR logging severity</summary>
        /// <param name="message">The value to write.</param>
        public void Error(object message)
        {
            Write(LogSeverity.ERROR, message);
        }
        
        /// <summary>Writes the specified object and its meta with ERROR logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="meta">Meta information of the record</param>
        public void Error(object message, LoggingMeta meta)
        {
            Write(LogSeverity.ERROR, message, meta);
        }

        /// <summary>Writes the specified object with CRITICAL logging severity</summary>
        /// <param name="message">The value to write.</param>
        public void Critical(object message)
        {
            Write(LogSeverity.CRITICAL, message);
        }
        
        /// <summary>Writes the specified object and its meta with CRITICAL logging severity</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="meta">Meta information of the record</param>
        public void Critical(object message, LoggingMeta meta)
        {
            Write(LogSeverity.CRITICAL, message, meta);
        }

        /// <summary>
        /// Check whether severity level is available
        /// </summary>
        /// <param name="level">Severity level</param>
        /// <returns>True if enabled, False if not</returns>
        public virtual bool IsLevelEnabled(LogSeverity level)
        {
            if (level < Severity)
                return false;
            if (level < parent.Severity)
                return false;
            return true;
        }

        /// <summary>Writes the specified object</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="severity">Severity of the record</param>
        public void Write(LogSeverity severity, object message)
        {
            Write(severity, message, LoggingMeta.Empty);
        }

        /// <summary>Writes the specified object</summary>
        /// <param name="message">The value to write.</param>
        /// <param name="severity">Severity of the record</param>
        /// <param name="meta">Meta information of the record</param>
        public void Write(LogSeverity severity, object message, LoggingMeta meta)
        {
            if (meta == null) throw new ArgumentNullException(nameof(meta));
            if (!IsLevelEnabled(severity))
                return;

            var mergedMeta = LoggingMeta.Merge(parent.Meta, this.Meta, meta);

            for(int i = 0; i < parent.Handlers.Count; i++)
                parent.Handlers[i].Write(severity, message, mergedMeta, this);

            for(int i = 0; i < handlers.Count; i++)
                handlers[i].Write(severity, message, mergedMeta, this);
        }
    }
}
