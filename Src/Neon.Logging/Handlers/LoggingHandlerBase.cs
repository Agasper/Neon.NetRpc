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

        /// <summary>
        /// Method to handle formatted string output
        /// </summary>
        /// <param name="message">Formatted log string</param>
        protected abstract void Write(string message);

        /// <summary>
        /// Handles the logging event
        /// </summary>
        /// <param name="severity">Log string severity</param>
        /// <param name="message">Logging object</param>
        /// <param name="meta">Final meta information</param>
        /// <param name="logger">Parent logger</param>
        public virtual void Write(LogSeverity severity, object message, LoggingMeta meta, ILogger logger)
        {
            if (Formatter == null)
                throw new NullReferenceException($"Handler {this.GetType().Name} has no formatter");
            this.Write(Formatter.Format(severity, message, meta, logger));
        }
    }
}
