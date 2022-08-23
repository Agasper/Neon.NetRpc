namespace Neon.Logging.Formatters
{
    public interface ILoggingFormatter
    {
        string Format(LogSeverity severity, object message, LoggingMeta meta, ILogger logger);
    }
}