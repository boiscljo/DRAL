﻿using AttentionAndRetag.Config;
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
using DRAL;

namespace AttentionAndRetag.Retag
{
    public class Retagger
    {
        public ConfigurationManager? ConfigurationManager { get; set; }

        public async Task<IMAGE_LABEL_INFO> ImproveLabel(FixedSizeImage? iActivated,
                                                         Image<Pixel>? img,
                                                         Image<byte>? gray,
                                                         IMAGE_LABEL_INFO label)
        {
            var ctx = img.Clone();

            Graphics<Pixel>? graphics=null;
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
                    Console.WriteLine("{2} MODE {0} {1}",p, wz, label.name);
                }
            }

            label = label.Clone();
            c.AdaptLabel(proposedBoxes, label, .9, .9, .25);
            if (iActivated != null)
                Application.Invoke((_, _1) =>
                {
                    iActivated.Image = ctx;
                });
            return label;
        }

        public void Init()
        {

        }
    }
}
