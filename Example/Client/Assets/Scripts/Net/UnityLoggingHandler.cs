using Neon.Logging;
using Neon.Logging.Formatters;
using Neon.Logging.Handlers;

namespace Neon.ClientExample.Net
{
    public class UnityLoggingHandler : ILoggingHandler
    {
        public ILoggingFormatter Formatter { get; set; }

        public UnityLoggingHandler()
        {
            LoggingFormatterDefault formatter = new LoggingFormatterDefault
            {
                IncludeLoggerNameInMessage = true,
                IncludeSeverityInMessage = true,
                IncludeTimestampInMessage = true
            };
            Formatter = formatter;
        }

        public void Write(LogSeverity severity, object message, LoggingMeta meta, ILogger logger)
        {
            string formatted = Formatter.Format(severity, message, meta, logger);
            switch (severity)
            {
                case LogSeverity.TRACE:
                case LogSeverity.DEBUG:
                case LogSeverity.INFO:
                    UnityEngine.Debug.Log(formatted);
                    break;
                case LogSeverity.WARNING:
                    UnityEngine.Debug.LogWarning(formatted);
                    break;
                case LogSeverity.ERROR:
                case LogSeverity.CRITICAL:
                    UnityEngine.Debug.LogError(formatted);
                    break;
                default:
                    UnityEngine.Debug.Log(formatted);
                    break;
            }
        }
    }
}