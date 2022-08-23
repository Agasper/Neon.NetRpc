using System;

namespace Neon.Logging
{
    public class Logger : ILogger
    {
        public LogSeverity Severity { get; set; }
        public string Name { get; set; }
        public LoggingMeta Meta => meta;
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

        public void Trace(object message)
        {
            Write(LogSeverity.TRACE, message);
        }
        public void Trace(object message, LoggingMeta meta)
        {
            Write(LogSeverity.TRACE, message, meta);
        }

        public void Debug(object message)
        {
            Write(LogSeverity.DEBUG, message);
        }
        public void Debug(object message, LoggingMeta meta)
        {
            Write(LogSeverity.DEBUG, message, meta);
        }

        public void Info(object message)
        {
            Write(LogSeverity.INFO, message);
        }
        public void Info(object message, LoggingMeta meta)
        {
            Write(LogSeverity.INFO, message, meta);
        }

        public void Warn(object message)
        {
            Write(LogSeverity.WARNING, message);
        }
        public void Warn(object message, LoggingMeta meta)
        {
            Write(LogSeverity.WARNING, message, meta);
        }

        public void Error(object message)
        {
            Write(LogSeverity.ERROR, message);
        }
        public void Error(object message, LoggingMeta meta)
        {
            Write(LogSeverity.ERROR, message, meta);
        }

        public void Critical(object message)
        {
            Write(LogSeverity.CRITICAL, message);
        }
        public void Critical(object message, LoggingMeta meta)
        {
            Write(LogSeverity.CRITICAL, message, meta);
        }

        public virtual bool IsLevelEnabled(LogSeverity level)
        {
            if (level < Severity)
                return false;
            if (level < parent.Severity)
                return false;
            return true;
        }

        public void Write(LogSeverity severity, object message)
        {
            Write(severity, message, LoggingMeta.Empty);
        }

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
