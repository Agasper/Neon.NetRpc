using Neon.Logging;
using Neon.Logging.Formatters;

namespace Neon.Test.Util;

public class NamedLoggingFormatter : LoggingFormatterDefault
{
    readonly string name; 
    
    public NamedLoggingFormatter(string name)
    {
        this.name = name ?? throw new ArgumentNullException(nameof(name));
    }
    
    public override string Format(LogSeverity severity, object message, LoggingMeta meta, ILogger logger)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        var stringBuilder = GetStringBuilder();
        stringBuilder.Clear();
        if (IncludeTimestampInMessage)
            stringBuilder.AppendFormat("[{0}] ", DateTime.UtcNow.ToString(DateTimeFormat));
        stringBuilder.AppendFormat("[{0}] ", name);
        if (IncludeLoggerNameInMessage)
            stringBuilder.AppendFormat("[{0}] ", logger.Name);
        if (IncludeSeverityInMessage)
            stringBuilder.Append($"[{severity}] ");
        stringBuilder.Append(message.ToString().Replace("\r", @"\r").Replace("\n", @"\n").Replace("\t", @"\t"));
        return stringBuilder.ToString();
    }
}