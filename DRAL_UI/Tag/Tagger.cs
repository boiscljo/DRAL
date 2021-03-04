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
    public class Tagger
    {
        public ConfigurationManager? ConfigurationManager { get; internal set; }

        internal async Task<List<RectangleF>> GenerateBoxes(FixedSizeImage iActivated,
                                                            Image<Pixel> img,
                                                            Image<byte> gray)
        {
            var ctx = img.Clone();

            MoyskleyTech.ImageProcessing.Image.Graphics<Pixel>? graphics = null;
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

            AttentionMapAnalizer c = new AttentionMapAnalizer();
            //Foreach config, keep proposed boxes
            foreach (var (p, wz) in StandardFunctions.ListXMeansModes(img))
            {
                proposedBoxes.AddRange(await c.Cluster(gray, graphics, p, wz));
                if (iActivated != null)
                    Application.Invoke((_, _1) =>
                    {
                        iActivated.Image = ctx;
                    });
                if (Program.verbose)
                {
                    Console.WriteLine("MODE {0} {1}", p, wz);
                }
            }

            return proposedBoxes;
        }
    }
}
