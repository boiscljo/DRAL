using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AttentionAndRetag.Retag
{
    public class StandardFunctions
    {
        public static Random r = new Random();

        public static Func<IVectorizable, IVectorizable, double> MinkowskiDistance(double p, double[] w)
        {
            return (a, b) =>
            {
                //Minkowski 
                IVectorizable v1 = a, v2 = b;
                var sum = 0.0;
                for (var i = 0; i < v1.Length; i++)
                {
                    sum += w[i] * Math.Pow(v2[i] - v1[i], p);
                }
                return Math.Abs(Math.Pow(sum, 1 / p));
            };
        }
        public static Func<IVectorizable, IVectorizable, double> MinkowskiDistance(double p)
        {
            return (a, b) =>
            {
                //Minkowski 
                return Math.Pow(a.Zip(b, (e1, e2) => Math.Pow(e1 - e2, p)).Sum(), 1 / p);
            };
        }
        public static T Identity<T>(T a)
        {
            return a;
        }
        public static Func<W, Y, A> Adapt<U, V, W, Y, Z, A>(Func<U, V, Z> f)
            where W : U
            where Y : V
            where Z : A
        {
            return (a, b) => f(a, b);
        }

        public static Func<int, Point3[]> GetRandomMeansFunction(Image<double> img)
        {
            Random r = new Random();
            return (int c) =>
            {
                return (from x in Enumerable.Range(0, c)
                        select new Point3(r.Next(img.Width), r.Next(img.Height), r.Next(256))).ToArray();
            };
        }

        public static Point3 GetMeanPt3(IEnumerable<Point3> arg)
        {
            if (arg.Count() > 1)
            {
                var mx = arg.WeightedAverage((x) => x.X, (x) => x.Z);
                var my = arg.WeightedAverage((x) => x.Y, (x) => x.Z);
                var mz = arg.Average((x) => x.Z);
                return new Point3(mx, my, mz);
            }
            else if (arg.Count() == 1)
                return arg.ElementAt(0);
            else
                return new Point3();
        }
        public static Pixel RandomColor()
        {
            return Pixel.FromArgb(255, (byte)r.Next(100, 256), (byte)r.Next(100, 256), (byte)r.Next(100, 256));
        }
        public static double IOU(RectangleF a, RectangleF b)
        {
            return Inter(a, b) / (a.Width * a.Height + b.Width * b.Height - Inter(a, b));
        }
        public static double IOS(RectangleF a, RectangleF b)
        {
            return Inter(a, b) / Math.Min(a.Width * a.Height, b.Width * b.Height);
        }

        public static double Inter(RectangleF boxA, RectangleF boxB)
        {
            var xA = Math.Max(boxA.X, boxB.X);

            var yA = Math.Max(boxA.Y, boxB.Y);

            var xB = Math.Min(boxA.Right, boxB.Right);

            var yB = Math.Min(boxA.Bottom, boxB.Bottom);

            var interArea = Math.Max(0, xB - xA + 1) * Math.Max(0, yB - yA + 1);
            return interArea;
        }
        public static Point[] Match25Selector(Point p)
        {
            return (from x in Enumerable.Range(-2, 5)
                    from y in Enumerable.Range(-2, 5)
                    where x != 0 && y != 0
                    select new Point(x + p.X, y + p.Y)).ToArray();
        }
        public static bool IsInside(Rectangle3 box1, Rectangle3 box2)
        {
            return (box1.Location.X > box2.Location.X &&
                box1.Location.Y > box2.Location.Y &&
                box1.End.X < box2.End.X &&
                box1.End.Y < box2.End.Y);
        }
        public static bool IsInside(RectangleF box1, RectangleF box2)
        {
            return (box1.X > box2.X &&
                box1.Y > box2.Y &&
                box1.Right < box2.Right &&
                box1.Bottom < box2.Bottom);
        }
        public static IEnumerable<T> Zip<K, T>(IEnumerable<IEnumerable<K>> src, Func<K[], T> fnc)
        {
            var arrays = src.ToList();

            if (arrays.Count == 1)
                arrays[0].Select((x) => fnc(new K[] { x }));
            if (arrays.Count == 2)
                return arrays[0].Zip(arrays[1], (a, b) => fnc(new K[] { a, b }));

            var tmp = arrays[0].Select((x) => new List<K>() { x });
            for (var i = 1; i < arrays.Count; i++)
            {
                tmp = tmp.Zip(arrays[i], (a, b) => { a.Append(b); return a; });
            }
            return tmp.Select((x) => fnc(x.ToArray()));
        }
    }
}
