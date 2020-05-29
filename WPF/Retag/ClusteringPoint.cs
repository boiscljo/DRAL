using System;
using System.Collections.Generic;
using System.Text;

namespace AttentionAndRetag.Retag
{
	public class ClusteringPoint<Point3>: IVectorizable
		where Point3 : IVectorizable,IClonable<Point3>
	{
		public int ClusterIndex;
		public Point3 Point;

		public ClusteringPoint(Point3 p, int cluserIndex)
		{
			ClusterIndex = cluserIndex;
			Point = p;
		}

		public double this[int idx] { get => Point[idx]; set => Point[idx] = value; }

		public int Length => Point.Length;

		public IEnumerable<T> Zip<T>(IVectorizable b, Func<double, double, T> func)
		{
			return Point.Zip(b, func);
		}
	}
}
