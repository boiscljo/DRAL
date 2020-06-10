using AttentionAndRetag.Config;
using AttentionAndRetag.Retag;
using DRAL.UI;
using Gtk;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DRAL.Tag
{
    class Tagger
    {
        public ConfigurationManager ConfigurationManager { get; internal set; }

        internal void Init()
        {
        }

        internal string GenerateFile(IMAGE_LABEL_INFO label, double w, double h)
        {
            return string.Empty;
        }

        internal async Task<List<RectangleF>> GenerateBoxes(FixedSizeImage iActivated, Image<Pixel> img, Image<byte> gray)
        {
            var ctx = img.Clone();

            MoyskleyTech.ImageProcessing.Image.Graphics<Pixel> graphics = null;
            if (iActivated != null)
            {
                graphics = MoyskleyTech.ImageProcessing.Image.Graphics.FromImage(ctx);
                Application.Invoke((_, _1) =>
                {
                    iActivated.Image = ctx;
                });
            }
            //ctx.Width = gray.Width;
            //ctx.Height = gray.Height;
            graphics?.Clear(Pixels.White);
            graphics?.DrawImage(img, 0, 0);


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
                if (iActivated != null)
                    Application.Invoke((_, _1) =>
                    {
                        iActivated.Image = ctx;
                    });
                if (Program.verbose)
                {
                    Console.WriteLine("MODE {0} {1}", cfg.p, cfg.wz);
                }
            }

            return proposedBoxes;
        }
    }
}
