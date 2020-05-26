using System;
using System.Collections.Generic;
using System.Linq;

namespace AttentionAndRetag.Retag
{
    public static class LINEEXT
    {
        public static double WeightedAverage<Y>(this IEnumerable<Y> y, Func<Y, double> val, Func<Y, double> weight)
        {
            double sum = y.Sum((x) => val(x) * weight(x));
            double totalWeight = y.Sum(weight);
            return sum / totalWeight;
        }
    }
}
