using System;
using Neon.Logging.Formatters;

namespace Neon.Logging.Handlers
{
    public abstract class LoggingHandlerBase : ILoggingHandler
    {
        public ILoggingFormatter Formatter { get; set; } = new LoggingFormatterDefault();

        public LoggingHandlerBase()
        {
        }

        public LoggingHandlerBase(ILoggingFormatter formatter)
        {
            this.Formatter = formatter;
        }

        protected abstract void Write(object message);

        public virtual void Write(LogSeverity severity, object message, LoggingMeta meta, ILogger logger)
        {
            if (Formatter == null)
                throw new NullReferenceException($"Handler {this.GetType().Name} has no formatter");
            this.Write(Formatter.Format(severity, message, meta, logger));
        }
    }
}
