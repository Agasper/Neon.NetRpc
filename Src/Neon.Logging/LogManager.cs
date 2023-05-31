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
        /// LogManager meta information, will be merged with Logger meta and record meta 
        /// </summary>
        public LoggingMeta Meta { get; private set; }
        /// <summary>
        /// LogManager handlers
        /// </summary>
        public LoggingHandlers Handlers => handlers;
        /// <summary>
        /// LogManager name filters
        /// </summary>
        public LoggingNameFilters LoggingNameFilters => filters;

        LoggingHandlers handlers;
        LoggingNameFilters filters;

        public LogManager(params ILoggingHandler[] handlers) : this()
        {
            this.handlers.Add(handlers);
        }

        public LogManager()
        {
            this.Meta = new LoggingMeta();
            this.handlers = new LoggingHandlers();
            this.filters = new LoggingNameFilters();
        }
        
        internal bool IsFiltered(ILogger logger, LogSeverity severity)
        {
            return filters.IsFiltered(logger, severity);
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
        
        /// <summary>
        /// Creates a new child instance of the logger 
        /// </summary>
        /// <param name="type">Logger type</param>
        /// <returns>New instance of the logger</returns>
        ILogger ILogManager.GetLogger(Type type)
        {
            return GetLogger(type.FullName);
        }

        /// <summary>
        /// Creates a new child instance of the logger 
        /// </summary>
        /// <param name="type">Logger type</param>
        /// <returns>New instance of the logger</returns>
        public virtual Logger GetLogger(Type type)
        {
            var logger = new Logger(type.FullName, this);
            return logger;
        }
    }
}
