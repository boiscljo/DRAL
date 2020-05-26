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
    }

    public class LABEL: IClonable<LABEL>
    {
        public BOX2D box2d { get; set; }
        public string category { get; set; }
        public List<POLY> poly2d { get; set; }
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
        public BOX2D Clone()
        {
            return DeserializeObject<BOX2D>(SerializeObject(this));
        }
    }
}
