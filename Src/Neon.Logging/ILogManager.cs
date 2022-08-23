using System.Collections.Generic;
using Neon.Logging.Handlers;

namespace Neon.Logging
{
    public interface ILogManager
    {
        LogSeverity Severity { get; }
        LoggingHandlers Handlers { get; }

        bool IsLevelEnabled(LogSeverity level);
        ILogger GetLogger(string name);
    }
}
