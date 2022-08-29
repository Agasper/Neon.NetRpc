using System.Collections.Generic;
using Neon.Logging.Handlers;

namespace Neon.Logging
{
    public interface ILogManager
    {
        /// <summary>
        /// LogManager severity
        /// </summary>
        LogSeverity Severity { get; }

        /// <summary>
        /// Check whether severity level is available
        /// </summary>
        /// <param name="level">Severity level</param>
        /// <returns>True if enabled, False if not</returns>
        bool IsLevelEnabled(LogSeverity level);
        
        /// <summary>
        /// Creates a new child instance of the logger 
        /// </summary>
        /// <param name="name">Logger name</param>
        /// <returns>New instance of the logger</returns>
        ILogger GetLogger(string name);
    }
}
