using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AttentionAndRetag.Retag
{
	/// <summary>
	/// Generic definition of a cluster for KMeans
	/// </summary>
	/// <typeparam name="PointType"></typeparam>
    public class Cluster<PointType> where PointType : IClonable<PointType>
	{
		public PointType Centroid { get; private set; }
		public IReadOnlyList<PointType> Data { get; private set; }

		public Cluster(PointType centroid,
                 IEnumerable<PointType> data)
		{
			Data = data.ToList().AsReadOnly();
			Centroid = centroid;
		}
	}
}
