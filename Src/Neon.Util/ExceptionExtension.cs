using System;

namespace Neon.Util
{
    public static class ExceptionExtension
    {
        /// <summary>
        /// Get the innermost exception via InnerException
        /// </summary>
        /// <param name="e">Exception</param>
        /// <returns>Innermost exception</returns>
        /// <exception cref="ArgumentNullException">if exception is null</exception>
        public static Exception GetInnermostException(this Exception e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            while (e.InnerException != null)
            {
                e = e.InnerException;
            }

            return e;
        }
        
        /// <summary>
        /// Get a default operation cancelled exception
        /// </summary>
        /// <returns></returns>
        public static OperationCanceledException GetOperationCancelledException()
        {
            return new OperationCanceledException("Operation cancelled");
        }

        /// <summary>
        /// Get a default timeout exception
        /// </summary>
        /// <returns>Timeout exception</returns>
        public static TimeoutException GetTimeoutException()
        {
            return new TimeoutException("Operation timed out");
        }
    }
}
