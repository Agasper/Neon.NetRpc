using System;
using System.Collections.Generic;
using Neon.Logging.Handlers;

namespace Neon.Logging
{
    public interface ILogManager
    {
        /// <summary>
        /// Creates a new child instance of the logger 
        /// </summary>
        /// <param name="name">Logger name</param>
        /// <returns>New instance of the logger</returns>
        ILogger GetLogger(string name);
        
        /// <summary>
        /// Creates a new child instance of the logger 
        /// </summary>
        /// <param name="type">Logger type</param>
        /// <returns>New instance of the logger</returns>
        ILogger GetLogger(Type type);
    }
}
