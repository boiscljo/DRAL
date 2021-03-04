using System.Collections.Generic;
using System.Linq;

namespace AttentionAndRetag.Retag
{
    public class Xmeans<PointType>
		where PointType : IClonable<PointType>,IVectorizable
	{
		public Kmeans<PointType> TopKm;
		private readonly int _k = 2;

		public Xmeans(List<PointType> points)
		{
			TopKm = new Kmeans<PointType>(_k, points);
		}

		private List<Kmeans<PointType>> RecursiveProcessing(Kmeans<PointType> parent,
                                                      List<Kmeans<PointType>> register)
		{
			var children = (from __k in Enumerable.Range(0,_k)
			select  new Kmeans<PointType>(_k, parent.ClusterPoints.Where(x => x.ClusterIndex == __k).Select(x => x.Point).ToList())).ToList();
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

		public List<Cluster<PointType>> Calculation()
		{
			TopKm.Calculation(1);
			var src = new List<Kmeans<PointType>>();
			var result = RecursiveProcessing(TopKm, src);

			return CreateClusterClass(result);
		}

		private List<Cluster<PointType>> CreateClusterClass(List<Kmeans<PointType>> result)
		{
			var pointsOfEachCluster = result.SelectMany((k) => from y in k.ClusterPoints.OrderBy((c)=>c.ClusterIndex).GroupBy((x) => x.ClusterIndex) select (from z in y select z.Point).ToList()).ToList();
			var centroids = result.SelectMany((kmeans)=>kmeans.CenterList).ToList();
			return pointsOfEachCluster.Zip(centroids, (pts, ctr) => new Cluster<PointType>(ctr, pts)).ToList();
		}
	}
}
