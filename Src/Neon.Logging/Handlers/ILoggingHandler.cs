namespace Neon.Logging.Handlers
{
    public interface ILoggingHandler
    {
        void Write(LogSeverity severity, object message, LoggingMeta meta, ILogger logger);
    }
}