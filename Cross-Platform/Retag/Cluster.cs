using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AttentionAndRetag.Retag
{
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
