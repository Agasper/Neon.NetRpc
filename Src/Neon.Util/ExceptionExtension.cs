using System;
namespace Neon.Util
{
    public static class ExceptionExtension
    {
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

        public static TimeoutException GetTimeoutException()
        {
            return new TimeoutException("Operation timed out");
        }
    }
}
