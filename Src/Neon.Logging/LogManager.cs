using System;
using System.Threading;
using Neon.Logging.Handlers;

namespace Neon.Logging
{
    public class LogManager : ILogManager
    {
        public static LogManager Default => logManagerDefault;
        public static LogManager Dummy => dummy.Value;

        static LogManager logManagerDefault;
        static Lazy<LogManager> dummy;

        static LogManager()
        {
            dummy = new Lazy<LogManager>(() =>
            {
                return new LogManager();
            }, LazyThreadSafetyMode.PublicationOnly);
            logManagerDefault = new LogManager();
        }

        public static void SetDefault(LogManager logManager)
        {
            if (logManager == null)
                throw new ArgumentNullException(nameof(logManager));
            logManagerDefault = logManager;
        }

        public LogSeverity Severity { get; set; }
        public LoggingMeta Meta { get; private set; }
        public LoggingHandlers Handlers => handlers;

        LoggingHandlers handlers;

        public LogManager(LogSeverity severity, params ILoggingHandler[] handlers) : this(severity)
        {
            this.handlers.Add(handlers);
        }

        public LogManager(LogSeverity severity) : this()
        {
            this.Severity = severity;
        }

        public LogManager()
        {
            this.Meta = new LoggingMeta();
            this.Severity = LogSeverity.TRACE;
            this.handlers = new LoggingHandlers();
        }

        public virtual bool IsLevelEnabled(LogSeverity level)
        {
            if (level < this.Severity)
                return false;
            return true;
        }

        ILogger ILogManager.GetLogger(string name)
        {
            return GetLogger(name);
        }

        public virtual Logger GetLogger(string name)
        {
            var logger = new Logger(name, this);
            return logger;
        }
    }
}
