using DRAL;
using System.Collections.Generic;
using System.Linq;
using static Newtonsoft.Json.JsonConvert;
namespace AttentionAndRetag.Retag
{
    /// <summary>
    /// Utility class for BDD label
    /// </summary>
    public class IMAGE_LABEL_INFO : IClonable<IMAGE_LABEL_INFO>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot change JSON files")]
        public string name { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot change JSON files")]
        public List<LABEL> labels { get; set; }

        public IMAGE_LABEL_INFO Clone()
        {
            return DeserializeObject<IMAGE_LABEL_INFO>(SerializeObject(this));
        }

        internal void Resize(double to_width, double to_height, int from_width, int from_height)
        {
            //to:400 from 200, xFactor 2
            var xFactor = to_width / from_width;
            var yFactor = to_height / from_height;
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

        internal string GenerateFile(double w, double h)
        {
            /*  case "traffic sign": return 0;
                case "traffic light": return 0;
                case "car": return 1;
                case "rider": return 2;
                case "motor": return 1;
                case "person": return 3;
                case "bus": return 1;
                case "truck": return 1;
                case "bike": return 2;
                case "train": return 1;*/
            //var newLabel = label.Clone();
            //newLabel.Resize(w, h, originalImageSize.Width, originalImageSize.Height);
            var possibleBox = this.labels.Where((x) => x.box2d != null).ToList();
            var possibleLines = possibleBox.Select((x) =>
            {
                return x.category + " " + Program.TS(x.box2d.x1 / w) + " " + Program.TS(x.box2d.y1 / h) + " " + Program.TS((x.box2d.x2 - x.box2d.x1) / w) + " " + Program.TS((x.box2d.y2 - x.box2d.y1) / h);
            });
            return string.Join("\r\n", possibleLines);

        }
    }
}
