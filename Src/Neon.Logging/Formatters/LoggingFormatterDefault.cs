using System;
using System.Text;
using System.Threading;

namespace Neon.Logging.Formatters
{
    public class LoggingFormatterDefault : ILoggingFormatter, IDisposable
    {
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
        public bool IncludeLoggerNameInMessage { get; set; } = true;
        public bool IncludeTimestampInMessage { get; set; } = true;
        public bool IncludeSeverityInMessage { get; set; } = true;

        ThreadLocal<StringBuilder> stringBuilders;

        public LoggingFormatterDefault()
        {
            stringBuilders = new ThreadLocal<StringBuilder>(() => new StringBuilder());
        }

        public void Dispose()
        {
            stringBuilders.Dispose();
        }

        protected StringBuilder GetStringBuilder()
        {
            return stringBuilders.Value;
        }

        /// <summary>
        /// Converts any incoming object with meta to the log string
        /// </summary>
        /// <param name="severity">Final row severity</param>
        /// <param name="message">Object to format</param>
        /// <param name="meta">Final meta information</param>
        /// <param name="logger">Parent logger</param>
        /// <returns>Log string</returns>
        public virtual string Format(LogSeverity severity, object message, Exception exception, LoggingMeta meta, ILogger logger)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            var stringBuilder = GetStringBuilder();
            try
            {
                if (IncludeTimestampInMessage)
                    stringBuilder.AppendFormat("[{0}] ", DateTime.UtcNow.ToString(DateTimeFormat));
                if (IncludeLoggerNameInMessage)
                    stringBuilder.AppendFormat("[{0}] ", logger.Name);
                if (IncludeSeverityInMessage)
                    stringBuilder.Append($"[{severity}] ");

                string finalMessage = message.ToString();
                if (exception != null)
                {
                    finalMessage += " -> " + exception.ToString();
                }

                stringBuilder.Append(finalMessage.ToString().Replace("\r", @"\r").Replace("\n", @"\n").Replace("\t", @"\t"));
                return stringBuilder.ToString();
            }
            finally
            {
                stringBuilder?.Clear();
            }
        }
    }
}
