using System;
using System.Collections.Generic;

namespace AttentionAndRetag.Retag
{
    /// <summary>
    /// Utility interface to .ZIP some points
    /// </summary>
    public interface IVectorizable
    {
        int Length { get; }
        double this[int idx] { get;set; }
        IEnumerable<T> Zip<T>(IVectorizable b,
                              Func<double, double, T> func);
    }
}