using DRAL.UI;
using Gtk;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.Text;

namespace DRAL.UI
{
    public class FixedSizeImage : Gtk.Image
    {
        public FixedSizeImage(Size siz)
        {
            this.WidthRequest = siz.Width;
            this.HeightRequest = siz.Height;
        }
        public Image<Pixel>? Image
        {
            set
            {
                if (value == null)
                    Pixbuf = null;
                else
                    Pixbuf = GtkWrapper.ToPixbuf(value).ScaleSimple(WidthRequest, HeightRequest, Gdk.InterpType.Bilinear);
            }
        }
    }
}
