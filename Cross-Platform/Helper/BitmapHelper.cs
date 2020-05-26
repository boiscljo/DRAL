using Gtk;
using ImageProcessing.JPEGCodec;
using MoyskleyTech.ImageProcessing;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace DRAL
{
    public static class BitmapHelper
    {
        public static Image<Pixel> Decode(this BitmapFactory facto, string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return facto.Decode(fs);
            }
        }
        public static void SaveJPG<T>(this Image<T> imp, string path)
            where T:unmanaged
        {
            using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                new JPEGCodec().Save<T>(imp, fs);
            }
        }
    }
}
