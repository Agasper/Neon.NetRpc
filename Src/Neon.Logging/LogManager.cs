using System;
using System.Threading;
using Neon.Logging.Handlers;

namespace Neon.Logging
{
    public class LogManager : ILogManager
    {
        /// <summary>
        /// A default static log manager helper
        /// </summary>
        public static ILogManager Default => logManagerDefault;
        /// <summary>
        /// An empty LogManager
        /// </summary>
        public static ILogManager Dummy => dummy.Value;

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

        /// <summary>
        /// Set the default LogManager
        /// </summary>
        /// <param name="logManager">LogManager to set</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="logManager" /> is <see langword="null" />.</exception>
        public static void SetDefault(LogManager logManager)
        {
            if (logManager == null)
                throw new ArgumentNullException(nameof(logManager));
            logManagerDefault = logManager;
        }

        /// <summary>
        /// LogManager severity
        /// </summary>
        public LogSeverity Severity { get; set; }
        /// <summary>
        /// LogManager meta information, will be merged with Logger meta and record meta 
        /// </summary>
        public LoggingMeta Meta { get; private set; }
        /// <summary>
        /// LogManager handlers
        /// </summary>
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

        /// <summary>
        /// Check whether severity level is available
        /// </summary>
        /// <param name="level">Severity level</param>
        /// <returns>True if enabled, False if not</returns>
        public virtual bool IsLevelEnabled(LogSeverity level)
        {
            if (level < this.Severity)
                return false;
            return true;
        }

        /// <summary>
        /// Creates a new child instance of the logger 
        /// </summary>
        /// <param name="name">Logger name</param>
        /// <returns>New instance of the logger</returns>
        ILogger ILogManager.GetLogger(string name)
        {
            return GetLogger(name);
        }

        /// <summary>
        /// Creates a new child instance of the logger 
        /// </summary>
        /// <param name="name">Logger name</param>
        /// <returns>New instance of the logger</returns>
        public virtual Logger GetLogger(string name)
        {
            var logger = new Logger(name, this);
            return logger;
        }
    }
}
