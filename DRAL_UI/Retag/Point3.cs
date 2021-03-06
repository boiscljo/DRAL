﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AttentionAndRetag.Retag
{
    /// <summary>
    /// Represent a 3D point
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = sizeof(double) * 3)]
    public struct Point3 : IClonable<Point3>, IVectorizable
    {
        [FieldOffset(0)]
        public double X;
        [FieldOffset(sizeof(double) * 1)]
        public double Y;
        [FieldOffset(sizeof(double) * 2)]
        public double Z;

        public Point3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public Point3 Clone()
        {
            return this;
        }

        public double[] ToVector() => new double[] { X, Y, Z };
        public void Deconstruct(out double x, out double y, out double z) => (x, y, z) = (X, Y, Z);

        public bool IsValid => !double.IsInfinity(X) && !double.IsInfinity(Y) && !double.IsInfinity(Z) &&
            !double.IsNaN(X) && !double.IsNaN(Y) && !double.IsNaN(Z);

        public int Length => 3;

        public double Get(int idx)
        {
            return idx switch
            {
                0 => X,
                1 => Y,
                2 => Z,
                _ => double.NaN
            };
        }
        public void Set(int idx,
                        double val)
        {
            if (idx == 0)
                X = val;
            if (idx == 1)
                Y = val;
            if (idx == 2)
                Z = val;
        }

        public IEnumerable<T> Zip<T>(IVectorizable b,
                                     Func<double, double, T> func)
        {
            for (var i = 0; i < Length; i++)
                yield return func(this[i], b[i]);
        }

        public double this[int idx] { get => Get(idx); set => Set(idx,value); }
    }
}
