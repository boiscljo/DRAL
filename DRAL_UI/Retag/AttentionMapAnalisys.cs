﻿using AttentionAndRetag.Config;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttentionAndRetag.Retag
{
    /// <summary>
    /// Analyzers for the attention map
    /// </summary>
    public class AttentionMapAnalizer
    {
        private const int MIN_ATTENTION_PERBYTE = 25;

        /// <summary>
        /// Hold the app configuration 
        /// </summary>
        public ConfigurationManager ConfigurationManager { get; set; }
        /// <summary>
        /// Separate the current grayscale attention map into clusters
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public List<Cluster<Point3>> SeparateImage(Image<byte> image)
        {
            Image<double> img = image.ConvertUsing<double>((px) => px);

            var lst = new List<List<Point3>>();
            //Converts to 3D points
            var pts = (from x in Enumerable.Range(0, image.Width)
                       from y in Enumerable.Range(0, image.Height)
                       where img[x, y] > MIN_ATTENTION_PERBYTE
                       select new Point3(x, y, img[x, y])).ToList();

            Kmeans<Point3>.GetInitialMeans = StandardFunctions.GetRandomMeansFunction(img);

            Xmeans<Point3> xmeans = new Xmeans<Point3>(pts);
            var clusters = xmeans.Calculation();

            return clusters;
        }
        /// <summary>
        /// Split non touching clusters
        /// </summary>
        /// <param name="clusters">Input list of clusters</param>
        /// <param name="minSize">Minimum size of a cluster to be considered</param>
        /// <returns>Output list of clusters</returns>
        public List<Cluster<Point3>> SplitNonTouching(List<Cluster<Point3>> clusters,
                                                      int minSize = 20)
        {
            List<Cluster<Point3>> workingSet = clusters.ToList();
            List<Rectangle3> rectangles = (from x in clusters select ToBox(x.Data)).ToList();
            for (var i = 0; i < workingSet.Count; i++)
            {
                //if (workingSet[i].Centroid.X == 0)
                //Console.WriteLine("");

                var box = ToBox(workingSet[i].Data);
                var tmp = Image<bool>.Create((int)rectangles[i].Size.X, (int)rectangles[i].Size.Y);
                tmp.ApplyFilter((pt, px) => false);
                foreach (var pt in workingSet[i].Data)
                    tmp[(int)(pt.X - rectangles[i].Location.X), (int)(pt.Y - rectangles[i].Location.Y)] = true;

                var subClusters = MoyskleyTech.ImageProcessing.Recognition.Border.ContourRecognition.AnalyseLargeImage<bool>(
                    tmp, (r) => r, MoyskleyTech.ImageProcessing.Recognition.Border.ContourRecognitionPointKeep.All,
                    StandardFunctions.Match25Selector);
                if (subClusters.Count > 1)
                {
                    workingSet.RemoveAt(i);
                    var rct = rectangles[i];
                    rectangles.RemoveAt(i);
                    i--;
                    for (var j = 0; j < subClusters.Count; j++)
                    {
                        if (subClusters[j].Points.Count > minSize)
                        {
                            workingSet.Add(new Cluster<Point3>(new Point3(), from x in subClusters[j].Points select new Point3(x.X + rct.Location.X, x.Y + rct.Location.Y, 0)));
                            rectangles.Add(new Rectangle3(subClusters[j].Area.X + rct.Location.X, subClusters[j].Area.Y + rct.Location.Y, 0, subClusters[j].Area.Width, subClusters[j].Area.Height, 0));
                        }
                    }
                }
            }
            return workingSet;
        }
        /// <summary>
        /// Initialize the analyzer
        /// </summary>
        public void Init()
        {
            Kmeans<Point3>.GetWitnesses = (cPoints) =>
            {
                var sumX = cPoints.Sum(x => x.X);
                var sumY = cPoints.Sum(x => x.Y);
                var sumZ = cPoints.Sum(x => x.Z);

                var sumX2 = cPoints.Sum(x => x.X * x.X);
                var sumY2 = cPoints.Sum(x => x.Y * x.Y);
                var sumZ2 = cPoints.Sum(x => x.Z * x.Z);

                var squX = sumX2 - (sumX * sumX / (double)cPoints.Count());
                var squY = sumY2 - (sumY * sumY / (double)cPoints.Count());
                var squZ = sumZ2 - (sumZ * sumZ / (double)cPoints.Count());

                return new double[] { squX, squY, squZ };
            };
            Kmeans<Point3>.GetDistance = StandardFunctions.Adapt<IVectorizable, IVectorizable, Point3, Point3, double, double>(
                StandardFunctions.MinkowskiDistance(3, new double[] { 1, 1, 1 }));
            Kmeans<Point3>.GetMean = StandardFunctions.GetMeanPt3;
        }
        /// <summary>
        /// Convert RectangleF to cls
        /// </summary>
        /// <param name="cls">Output</param>
        /// <param name="matchedBox">Input</param>
        private void SetBox(BOX2D cls,
                            in RectangleF matchedBox)
        {
            cls.x1 = matchedBox.X;
            cls.y1 = matchedBox.Y;
            cls.x2 = matchedBox.Right;
            cls.y2 = matchedBox.Bottom;
        }
        /// <summary>
        /// Improves the labels
        /// </summary>
        /// <param name="proposedBoxes">Cluster list</param>
        /// <param name="labels">Original label</param>
        /// <param name="IOU_THRESHOLD">IOU_THRESHOLD</param>
        /// <param name="IOS_THRESHOLD">IOS_THRESHOLD</param>
        /// <param name="maxShrink">maxShrink</param>
        public void AdaptLabel(List<RectangleF> proposedBoxes,
                               IMAGE_LABEL_INFO labels,
                               double IOU_THRESHOLD = 0.9,
                               double IOS_THRESHOLD = 0.9,
                               double maxShrink = 0.25)
        {
            foreach (var cls in labels.labels)//foreach label
            {
                var box = cls.box2d;
                string className = cls.category;
                var color = StandardFunctions.RandomColor;
                if (box != null)
                {
                    double x1 = box.x1, x2 = box.x2, y1 = box.y1, y2 = box.y2;
                    var rct = new RectangleF(x1, y1, x2 - x1 + 1, y2 - y1 + 1);
                    //step 1: Does the box fit any in proposed

                    var matchedBox = proposedBoxes
                        .Select((x) => new { box = x, iou = StandardFunctions.IOU(x, rct) })
                        .Where((x) => x.iou > IOU_THRESHOLD)
                        .OrderBy((x) => x.iou)
                        .FirstOrDefault()?.box;

                    if (matchedBox != null)
                    {
                        SetBox(box, matchedBox.Value);
                        continue;
                    }
                    //step 2:expand box to fit
                    var matchedBoxesIOS = proposedBoxes
                        .Select((x) => new { box = x, ios = StandardFunctions.IOS(x, rct) })
                        .Where((x) => x.box.Width * x.box.Height < rct.Width * rct.Height)//matched must be smaller
                        .Where((x) => x.ios > IOS_THRESHOLD)
                        .OrderBy((x) => x.ios).ToArray();
                    if (matchedBoxesIOS.Length > 0)
                    {
                        var left = Math.Min(x1, matchedBoxesIOS.Min((x) => x.box.Left));
                        var right = Math.Max(x2, matchedBoxesIOS.Max((x) => x.box.Right));

                        var top = Math.Min(y1, matchedBoxesIOS.Min((x) => x.box.Top));
                        var bottom = Math.Max(y2, matchedBoxesIOS.Max((x) => x.box.Bottom));

                        rct = new RectangleF(left, top, right - left + 1, bottom - top + 1);
                    }

                    //step 3:boxes inside
                    var matchedBoxesInside = proposedBoxes
                        .Where((x) => StandardFunctions.IsInside(x, rct))
                        .ToArray();

                    if (matchedBoxesInside.Length > 0)
                    {
                        var left = matchedBoxesInside.Min((x) => x.Left);
                        var right = matchedBoxesInside.Max((x) => x.Right);

                        var top = matchedBoxesInside.Min((x) => x.Top);
                        var bottom = matchedBoxesInside.Max((x) => x.Bottom);

                        var propRct = new RectangleF(left, top, right - left + 1, bottom - top + 1);

                        if (propRct.Width * propRct.Height >= rct.Width * rct.Height * (1 - maxShrink))
                            rct = propRct;
                    }
                    SetBox(box, rct);
                    //Step 4:
                    //Do nothing just keep it

                }
                else if (cls.poly2d != null)
                {
                    //Do nothing, we won't edit polys
                }
            }
        }
        /// <summary>
        /// Cluster the image
        /// </summary>
        /// <param name="image">Input attention map</param>
        /// <param name="graphics">Graphics to display while processing, can be null to disable</param>
        /// <param name="p">P parameter for minkowski</param>
        /// <param name="wz">Weight for the Z parameter</param>
        /// <returns></returns>
        public async Task<IEnumerable<RectangleF>> Cluster(Image<byte> image,
                                                           Graphics<Pixel>? graphics,
                                                           int p,
                                                           double wz)
        {
            Kmeans<Point3>.GetDistance = StandardFunctions.Adapt<IVectorizable, IVectorizable, Point3, Point3, double, double>(
            StandardFunctions.MinkowskiDistance(p, new double[] { 1, 1, wz }));
            var clusters = await Task.Run(() => SeparateImage(image));
            clusters = SplitNonTouching(clusters);
            //clusters = MergeBox(clusters);

            var colors = (from x in clusters select StandardFunctions.RandomColor).ToList();
            var clustersWithColor = clusters.Zip(colors);
            if (graphics != null)
            {
                Image<Pixel> working = image.ConvertTo<Pixel>();
                //Each pixel
                foreach (var (First, Second) in clustersWithColor)
                {
                    var cluster = First;
                    var color = Second;

                    foreach (var pixel in cluster.Data)
                    {
                        if (graphics != null)
                            working[(int)pixel.X, (int)pixel.Y] = color;
                    }
                }
                graphics.DrawImage(working, 0, 0);
                //Boxes
                foreach (var (cluster, color) in clustersWithColor)
                {
                    var box = ToBox(cluster.Data);
                    if (box.IsValid)
                        graphics.DrawRectangle(color, box.Location.X, box.Location.Y, box.Size.X, box.Size.Y, 5);
                }
            }
            graphics?.DrawString("p=" + p + ",wz=" + wz, Pixels.DeepPink, 0, 0, BaseFonts.Premia, 3);
            var boxes = ToBox(clusters);
            return To2D(boxes);
        }
        /// <summary>
        /// Convert 3D box to 2D box
        /// </summary>
        /// <param name="boxes">Input 3d Rectangle</param>
        /// <returns>2d rectangle</returns>
        public IEnumerable<RectangleF> To2D(List<Rectangle3> boxes)
        {
            return from bx in boxes
                   select new RectangleF(bx.Location.X, bx.Location.Y, bx.Size.X, bx.Size.Y);
        }
        /// <summary>
        /// Convert Clusters into Rectangle3D
        /// </summary>
        /// <param name="clusters">Input clusters</param>
        /// <returns>3d rectangle</returns>
        public List<Rectangle3> ToBox(IEnumerable<Cluster<Point3>> clusters)
        {
            List<Rectangle3> lst = new List<Rectangle3>();
            foreach (var cluster in clusters)
            {
                if (cluster.Data.Count() > 0)
                {
                    Rectangle3 rct = ToBox(cluster.Data);

                    lst.Add(rct);
                }
            }
            return lst;
        }
        /// <summary>
        /// Calculate bounds of a blob
        /// </summary>
        /// <param name="cluster">List of all pixels</param>
        /// <returns>Bounding box</returns>
        public static Rectangle3 ToBox(IEnumerable<Point3> cluster)
        {
            var stat = MoyskleyTech.Mathematics.Statistics.DescriptiveStatistics.From2D(
                                    from x in cluster
                                    select new MoyskleyTech.Mathematics.Coordinate(x.X, x.Y)
                                    );
            var avgZ = cluster.Average((x) => x.Z);
            var rct = new Rectangle3(
                stat.MinX, stat.MinY, avgZ,
                stat.RangeX, stat.RangeY, 1
                );
            return rct;
        }
        /// <summary>
        /// Merge box step
        /// </summary>
        /// <param name="clusters">Input clusters</param>
        /// <returns>Output clusters</returns>
        public List<Cluster<Point3>> MergeBox(List<Cluster<Point3>> clusters)
        {
            var rectangles = (from x in clusters select ToBox(x.Data)).ToList();
            var workingSet = clusters.ToList();

            for (var i = 0; i < workingSet.Count; i++)
                for (var j = 0; j < workingSet.Count; j++)
                {
                    if (StandardFunctions.IsInside(rectangles[i], rectangles[j]))//IsInside
                    {
                        //bool isTouching = AreClusterTouching(workingSet[i], workingSet[j], rectangles[i]);
                        //if (isTouching)
                        {
                            workingSet[j] = new Cluster<Point3>(workingSet[j].Centroid,
                                workingSet[j].Data.Concat(workingSet[i].Data));//Merge clusters
                            //Don't need to ajust, j is already larger
                            //rectangles[j] = ToBox(workingSet[j].Data);
                            workingSet.RemoveAt(i);
                            rectangles.RemoveAt(i);
                            i = -1;
                            j = workingSet.Count;
                        }
                    }
                }

            return workingSet;
        }
        /// <summary>
        /// Detection if clusters are touching
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="b1"></param>
        /// <returns></returns>
        public static bool AreClusterTouching(Cluster<Point3> a,
                                              Cluster<Point3> b,
                                              Rectangle3 b1)//b is larger
        {
            static Point ToPt(Point3 pt)
            {
                return new Point((int)pt.X, (int)pt.Y);
            }
            IEnumerable<Point> pt1 = a.Data.Select(ToPt).ToArray();
            var end = b1.End;
            IEnumerable<Point> pt2 = b.Data.Where((pt) =>
            {
                return
                pt.X >= b1.Location.X - 1 &&
                pt.Y >= b1.Location.Y - 1 &&
                pt.X <= end.X &&
                pt.Y <= end.Y;
            }).Select(ToPt).ToHashSet();//Reduce to only 1 larger than smallest and make hashable for faster search

            return pt1.Any((pt) =>
            {
                return StandardFunctions.Match25Selector(pt).Any((s) => pt2.Contains(s));
            });

        }
    }
}
