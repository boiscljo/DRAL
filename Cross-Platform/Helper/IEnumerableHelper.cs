using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace DRAL.Helper
{
    public static class IEnumerableHelper
    {
        public static T ElementAtOrFallback<T>(this IEnumerable<T> src,
                                               int idx,
                                               T fallback)
        {
            try
            {
                return src.ElementAt(idx);
            }
            catch
            {
                return fallback;
            }
        }
    }
}