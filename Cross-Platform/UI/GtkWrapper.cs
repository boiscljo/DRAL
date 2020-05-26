using Cairo;
using Gdk;
using Gtk;
using Hjg.Pngcs;
using MoyskleyTech.ImageProcessing;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DRAL.UI
{
    public class GtkWrapper 
    {
        public static Pixbuf ToPixbuf<T>(Image<T> input)
            where T: unmanaged
        {
            MemoryStream ms = new MemoryStream();
            input.Save(ms);
            ms.Position = 0;
            return new Pixbuf(ms);
        }
    }
}
