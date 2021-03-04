using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Text;
using static Newtonsoft.Json.JsonConvert;
namespace AttentionAndRetag.Retag
{
    /// <summary>
    /// Utility class for BDD label
    /// </summary>
    public class BOX2D : IClonable<BOX2D>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot change JSON files")]
        public double x1 { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot change JSON files")]
        public double y1 { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot change JSON files")]
        public double x2 { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot change JSON files")]
        public double y2 { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot change JSON files")]
        public double width => x2 - x1;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot change JSON files")]
        public double height => y2 - y1;

        public BOX2D Clone()
        {
            return DeserializeObject<BOX2D>(SerializeObject(this));
        }

        internal void SetLocation(PointF pos)
        {
            x1 = pos.X;
            y1 = pos.Y;
        }

        internal void SetEnd(PointF pos)
        {
            x2 = pos.X;
            y2 = pos.Y;
        }

        internal void Fix(int width, int height)
        {
            double tmp;
            if (x2 < x1)
            {
                tmp = x2;
                x2 = x1;
                x1 = tmp;
            }
            if (y2 < y1)
            {
                tmp = y2;
                y2 = y1;
                y1 = tmp;
            }
            if (x1 > width)
                x1 = width - 1;
            if (x1 < 0)
                x1 = 0;
            if (x2 > width)
                x2 = width - 1;
            if (x2 < 0)
                x2 = 0;
            if (y1 > height)
                y1 = height - 1;
            if (y1 < 0)
                y1 = 0;
            if (y2 > height)
                y2 = height - 1;
            if (y2 < 0)
                y2 = 0;
        }

        internal void Scale(double xFactor, double yFactor)
        {
            x1 *= xFactor;
            x2 *= xFactor;
            y1 *= yFactor;
            y2 *= yFactor;
        }
    }
}
