using System.Collections.Generic;

namespace Neon.Util
{
    static class EnumerationHelper
    {
        public static IEnumerable<T> SingleItemAsEnumerable<T>(this T item)
        {
            yield return item;
        }
    }
}
