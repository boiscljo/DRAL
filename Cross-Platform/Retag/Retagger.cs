using AttentionAndRetag.Config;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using Gtk;
using DRAL.UI;

namespace AttentionAndRetag.Retag
{
    public class Retagger
    {
        public ConfigurationManager ConfigurationManager { get; set; }

        public async Task<IMAGE_LABEL_INFO> ImproveLabel(FixedSizeImage iActivated,Image<Pixel> img, Image<byte> gray, IMAGE_LABEL_INFO label)
        {
            var ctx = img.Clone();
            MoyskleyTech.ImageProcessing.Image.Graphics<Pixel> graphics = MoyskleyTech.ImageProcessing.Image.Graphics.FromImage(ctx);
            //ctx.Width = gray.Width;
            //ctx.Height = gray.Height;
            graphics.Clear(Pixels.White);
            graphics.DrawImage(img, 0, 0);
            iActivated.Image = ctx;
            List<RectangleF> proposedBoxes = new List<RectangleF>();

            AttentionMapAnaliser c = new AttentionMapAnaliser();
            //Foreach config, keep proposed boxes
            foreach (var cfg in new CFG[] {
                    new CFG(){ p=1, wz=3},
                    new CFG(){ p=1, wz=img.Width/255f},
                    new CFG(){ p=4, wz=3},
                    new CFG(){ p=4, wz=4},
                    new CFG(){ p=4, wz=img.Width/255f}
                    })
            {
                proposedBoxes.AddRange(await c.Cluster(gray.ConvertTo<Pixel>(), graphics, cfg.p, cfg.wz));
                iActivated.Image = ctx;
            }

            label = label.Clone();
            c.AdaptLabel(proposedBoxes, label, .9, .9, .25);
            iActivated.Image = ctx;
            return label;
        }
        public string GenerateFile(IMAGE_LABEL_INFO label,double w, double h)
        {
            var possibleBox = label.labels.Where((x) => x.box2d != null && ConfigurationManager.IsKnownCategory(x.category)).ToList();
            var possibleLines = possibleBox.Select((x) =>
            {
                return ConfigurationManager.GETKnownCategoryID(x.category) + " " + TS(x.box2d.x1 / w) + " " + TS(x.box2d.y1 / h) + " " + TS((x.box2d.x2 - x.box2d.x1) / w) + " " + TS((x.box2d.y2 - x.box2d.y1) / h);
            });
            return string.Join("\r\n", possibleLines);
        }
        private string TS(double v)
        {
            return v.ToString(new CultureInfo("en-US"));
        }

        public void Init()
        {
            
        }
    }
}
