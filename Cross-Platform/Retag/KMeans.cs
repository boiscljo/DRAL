using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AttentionAndRetag.Retag
{

	public class Kmeans<T>
		where T:IClonable<T>, IVectorizable
	{
		public int K;
		public List<ClusteringPoint<T>> ClusterPoints = new List<ClusteringPoint<T>>();
		public List<T> CenterList = new List<T>();
		public List<T> BeforeCenterList = new List<T>();
		public List<double> CenterDistAvg = new List<double>();
		public List<double> CenterDistDispersion = new List<double>();
		
		public List<double> BICs = new List<double>();
		public static Func<int,T[]> GetInitialMeans;
		public static Func<IEnumerable<T>,T> GetMean;
		public static Func<T, T, double> GetDistance;
		public static Func<IEnumerable<T>, double[]> GetWitnesses;
		private const int maxLoopCount=1000;

		public Kmeans(int k, List<T> points)
		{
			K = k;
			var rnd = new Random();
			foreach (var p in points)
			{
				//First is random
				var cluster = rnd.Next(k);
				ClusterPoints.Add(new ClusteringPoint<T>(p, cluster));
			}

			for (int i = 0; i < K; i++)
			{
				CenterDistAvg.Add(int.MaxValue);
				CenterDistDispersion.Add(int.MaxValue);
				BICs.Add(int.MaxValue);
				
			}
			CenterList.AddRange(GetInitialMeans(k));
			BeforeCenterList = new List<T>(CenterList);
		}

		private void OneAct()
		{
			//Update Center
			for (int i = 0; i < K; i++)
			{
				BeforeCenterList[i] = CenterList[i].Clone();

				var cv = ClusterPoints.Where(x => x.ClusterIndex == i);
				if (cv.Count() > 0)
				{
					CenterList[i] = GetMean(from x in cv select x.Point);
				}
				//error exception
				else
				{
					//BeforeCenterList[i] = new T(int.MaxValue, int.MaxValue, int.MaxValue);
					//CenterList[i] = new T(int.MaxValue, int.MaxValue, int.MaxValue);
				}
			}

			//Update Nearest Cluster
			foreach (var p in ClusterPoints)
			{
				var updateIndex = GetNearestIndex(p.Point, CenterList);
				p.ClusterIndex = updateIndex;
			}

			//Update Avg Center Dist
			for (int i = 0; i < K; i++)
			{
				var cPoints = ClusterPoints.Where(x => x.ClusterIndex == i);

				//error exception
				if (cPoints.Count() == 0)
				{
					CenterDistAvg[i] = int.MaxValue;
					CenterDistDispersion[i] = int.MaxValue;
					BICs[i] = int.MaxValue;
					continue;
				}

				var list = new List<double>();
				foreach (var p in cPoints)
				{
					var dist = GetDistance(CenterList[i], p.Point);
					list.Add(dist);
				}
				CenterDistAvg[i] = list.Average();

				var sum = 0.0;
				foreach (var l in list)
				{
					sum += Math.Pow((l - CenterDistAvg[i]), 2);
				}
				CenterDistDispersion[i] = sum / (double)list.Count;

				//Cal BICs
				//http://stackoverflow.com/questions/15839774/how-to-calculate-bic-for-k-means-clustering-in-r

				//cal witness
				

				var witnesses = GetWitnesses(from x in cPoints select x.Point);
				
				var totWitness = witnesses.Sum();

				var m = 2; //point dimension
				var n = cPoints.Count();
				var d = totWitness;
				BICs[i] = d + Math.Log(n) * m * K;
			}
		}

		public int Calculation(double permitErrorDist)
		{
			var loopCount = 0;
			while (loopCount < maxLoopCount)
			{
				loopCount++;
				OneAct();
				var ok = true;
				//judge all center change value is less than error dist
				for (int i = 0; i < CenterList.Count; i++)
				{
					var dist = GetDistance(CenterList[i], BeforeCenterList[i]);

					if (permitErrorDist < dist)
					{
						ok = false;
						break;
					}
				}
				if (ok) break;
			}
			return loopCount;
		}

		private int GetNearestIndex(T p,
                              List<T> centerList)
		{
			var value = GetDistance(CenterList[0], p);
			var index = 0;

			for (int i = 1; i < centerList.Count; i++)
			{
				var v = GetDistance(CenterList[i],p);
				if (v < value)
				{
					value = v;
					index = i;
				}
			}
			return index;
		}
	}
}
