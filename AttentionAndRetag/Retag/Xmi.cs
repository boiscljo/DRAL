using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AttentionAndRetag.Retag
{
	public class Xmeans<Point3>
		where Point3 : IClonable<Point3>,IVectorizable
	{
		public Kmeans<Point3> TopKm;
		private int _k = 2;

		public Xmeans(List<Point3> points)
		{
			TopKm = new Kmeans<Point3>(_k, points);
		}

		private List<Kmeans<Point3>> RecursiveProcessing(Kmeans<Point3> parent, List<Kmeans<Point3>> register)
		{
			var children = (from __k in Enumerable.Range(0,_k)
			select  new Kmeans<Point3>(_k, parent.ClusterPoints.Where(x => x.ClusterIndex == __k).Select(x => x.Point).ToList())).ToList();
			foreach (var child in children)
				child.Calculation(1);
			var selectedChild = false;
			for (var i = 0; i < _k&& !selectedChild; i++)
			{
				var child = children[i];
				if (parent.CenterDistDispersion[i] > child.CenterDistDispersion.Sum()
					&& child.ClusterPoints.Count > 1)
				{
					RecursiveProcessing(child, register);
				}
				else
					register.Add(child);
			}

			return register;
		}

		public List<Cluster<Point3>> Calculation()
		{
			TopKm.Calculation(1);
			var src = new List<Kmeans<Point3>>();
			var result = RecursiveProcessing(TopKm, src);

			return CreateClusterClass(result);
		}

		private List<Cluster<Point3>> CreateClusterClass(List<Kmeans<Point3>> result)
		{
			var pointsOfEachCluster = result.SelectMany((k) => from y in k.ClusterPoints.OrderBy((c)=>c.ClusterIndex).GroupBy((x) => x.ClusterIndex) select (from z in y select z.Point).ToList()).ToList();
			var centroids = result.SelectMany((kmeans)=>kmeans.CenterList).ToList();
			return pointsOfEachCluster.Zip(centroids, (pts, ctr) => new Cluster<Point3>(ctr, pts)).ToList();
		}
	}

	public class Cluster<Point3> where Point3 : IClonable<Point3>
	{
		public Point3 Centroid { get; private set; }
		public IReadOnlyList<Point3> Data { get; private set; }

		public Cluster(Point3 centroid, IEnumerable<Point3> data)
		{
			Data = data.ToList().AsReadOnly();
			Centroid = centroid;
		}
	}
}
