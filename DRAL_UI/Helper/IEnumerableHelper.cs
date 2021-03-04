using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace DRAL.Helper
{
    /// <summary>
    /// Contains all extention method for IEnumerable
    /// </summary>
    public static class IEnumerableHelper
    {
        /// <summary>
        /// Equivalent to first or default, but default is replaced by fallback in case of error
        /// </summary>
        /// <typeparam name="T">Type of IEnumerable</typeparam>
        /// <param name="src">IEnumerable</param>
        /// <param name="idx">Index of desired element</param>
        /// <param name="fallback">Fallback value if error</param>
        /// <returns></returns>
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