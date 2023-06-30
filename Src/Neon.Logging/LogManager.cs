using System;
using System.Threading;
using Neon.Logging.Handlers;

namespace Neon.Logging
{
    public class LogManager : ILogManager
    {
        static LogManager _logManagerDefault;
        static readonly Lazy<LogManager> _dummy;

        /// <summary>
        ///     A default static log manager helper
        /// </summary>
        public static LogManager Default => _logManagerDefault;

        /// <summary>
        ///     An empty LogManager
        /// </summary>
        public static LogManager Dummy => _dummy.Value;

        /// <summary>
        ///     LogManager meta information, will be merged with Logger meta and record meta
        /// </summary>
        public LoggingMeta Meta { get; private set; }

        /// <summary>
        ///     LogManager handlers
        /// </summary>
        public LoggingHandlers Handlers { get; }

        /// <summary>
        ///     LogManager name filters
        /// </summary>
        public LoggingNameFilters LoggingNameFilters { get; }

        static LogManager()
        {
            _dummy = new Lazy<LogManager>(() => { return new LogManager(); }, LazyThreadSafetyMode.PublicationOnly);
            _logManagerDefault = new LogManager();
        }

        public LogManager(params ILoggingHandler[] handlers) : this()
        {
            Handlers.Add(handlers);
        }

        public LogManager()
        {
            Meta = new LoggingMeta();
            Handlers = new LoggingHandlers();
            LoggingNameFilters = new LoggingNameFilters();
        }

        /// <summary>
        ///     Creates a new child instance of the logger
        /// </summary>
        /// <param name="name">Logger name</param>
        /// <returns>New instance of the logger</returns>
        ILogger ILogManager.GetLogger(string name)
        {
            return GetLogger(name);
        }

        /// <summary>
        ///     Creates a new child instance of the logger
        /// </summary>
        /// <param name="type">Logger type</param>
        /// <returns>New instance of the logger</returns>
        ILogger ILogManager.GetLogger(Type type)
        {
            return GetLogger(type.FullName);
        }
        //
        // /// <summary>
        // ///     Set the default LogManager
        // /// </summary>
        // /// <param name="logManager">LogManager to set</param>
        // /// <exception cref="T:System.ArgumentNullException"><paramref name="logManager" /> is <see langword="null" />.</exception>
        // public static void SetDefault(LogManager logManager)
        // {
        //     if (logManager == null)
        //         throw new ArgumentNullException(nameof(logManager));
        //     _logManagerDefault = logManager;
        // }

        /// <summary>
        /// Check is the record with specified severity will be filtered
        /// </summary>
        /// <param name="severity">Severity</param>
        /// <returns>true is log entry will be ignored</returns>
        internal bool IsFiltered(ILogger logger, LogSeverity severity)
        {
            return LoggingNameFilters.IsFiltered(logger, severity);
        }

        /// <summary>
        ///     Creates a new child instance of the logger
        /// </summary>
        /// <param name="name">Logger name</param>
        /// <returns>New instance of the logger</returns>
        public virtual Logger GetLogger(string name)
        {
            var logger = new Logger(name, this);
            return logger;
        }

        /// <summary>
        ///     Creates a new child instance of the logger
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