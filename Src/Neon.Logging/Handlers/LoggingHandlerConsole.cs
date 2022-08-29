using System;
using Neon.Logging.Formatters;

namespace Neon.Logging.Handlers
{
    public class LoggingHandlerConsole : LoggingHandlerBase
    {
        public LoggingHandlerConsole()
        {
        }

        public LoggingHandlerConsole(ILoggingFormatter formatter) : base(formatter)
        {
        }

        protected override void Write(string message)
        {
            Console.WriteLine(message);
        }
    }
}
