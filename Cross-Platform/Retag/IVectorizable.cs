using System;
using System.Collections.Generic;

namespace AttentionAndRetag.Retag
{
    public interface IVectorizable
    {
        int Length { get; }
        double this[int idx] { get;set; }
        IEnumerable<T> Zip<T>(IVectorizable b,
                              Func<double, double, T> func);
    }
}