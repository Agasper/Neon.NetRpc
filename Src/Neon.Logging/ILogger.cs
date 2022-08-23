using System.Collections.Generic;
using Neon.Logging.Handlers;

namespace Neon.Logging
{
    public interface ILogger
    {
        LogSeverity Severity { get; set; }
        string Name { get; set;  }
        LoggingMeta Meta { get; }
        LoggingHandlers Handlers { get; }

        void Trace(object message);
        void Trace(object message, LoggingMeta meta);
        void Debug(object message);
        void Debug(object message, LoggingMeta meta);
        void Info(object message);
        void Info(object message, LoggingMeta meta);
        void Warn(object message);
        void Warn(object message, LoggingMeta meta);
        void Error(object message);
        void Error(object message, LoggingMeta meta);
        void Critical(object message);
        void Critical(object message, LoggingMeta meta);

        void Write(LogSeverity severity, object message);
        void Write(LogSeverity severity, object message, LoggingMeta meta);

        bool IsLevelEnabled(LogSeverity level);
    }
}
