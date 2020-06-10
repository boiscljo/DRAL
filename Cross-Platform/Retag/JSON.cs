using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.Text;
using static Newtonsoft.Json.JsonConvert;
namespace AttentionAndRetag.Retag
{
    public class LABEL_FILE: List<IMAGE_LABEL_INFO>, IClonable<LABEL_FILE>
    {
        public LABEL_FILE Clone()
        {
            return DeserializeObject<LABEL_FILE>(SerializeObject(this));
        }
    }
    public class IMAGE_LABEL_INFO: IClonable<IMAGE_LABEL_INFO>
    {
        public string name { get; set; }
        public List<LABEL> labels { get; set; }

        public IMAGE_LABEL_INFO Clone()
        {
            return DeserializeObject<IMAGE_LABEL_INFO>(SerializeObject(this));
        }

        internal void Resize(double w, double h, int width, int height)
        {
            var xFactor = w / width;
            var yFactor = h / height;
            foreach (var lbl in labels)
            {
                if (lbl.box2d != null)
                {
                    lbl.box2d.Scale(xFactor, yFactor);
                }
                if (lbl.poly2d != null)
                    foreach (var poly in lbl.poly2d)
                    {
                        foreach (var vert in poly.vertices)
                        {
                            vert[0] *= xFactor;
                            vert[1] *= yFactor;
                        }
                    }
            }
        }
    }

    public class LABEL: IClonable<LABEL>
    {
        public BOX2D box2d { get; set; } = new BOX2D();
        public string category { get; set; }
        public List<POLY> poly2d { get; set; } = new List<POLY>();
        public LABEL Clone()
        {
            return DeserializeObject<LABEL>(SerializeObject(this));
        }
    }

    public class POLY: IClonable<POLY>
    {
        public List<List<double>> vertices { get; set; }
        public POLY Clone()
        {
            return DeserializeObject<POLY>(SerializeObject(this));
        }
    }

    public class BOX2D: IClonable<BOX2D>
    {
        public double x1 { get; set; }
        public double y1 { get; set; }
        public double x2 { get; set; }
        public double y2 { get; set; }
        public double width => x2 - x1 + 1;
        public double height => y2 - y1 + 1;

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

        internal void Fix()
        {
            double tmp;
            if (x2 < x1)
            {   tmp = x2;
                x2 = x1;
                x1 = tmp;
            }
            if (y2 < y1)
            {
                tmp = y2;
                y2 = y1;
                y1 = tmp;
            }
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
