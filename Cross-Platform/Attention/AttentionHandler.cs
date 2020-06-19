using AttentionAndRetag.Retag;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using AttentionAndRetag.Config;
using DRAL;

namespace AttentionAndRetag.Attention
{
    public class AttentionHandler
    {
        public ConfigurationManager ConfigurationManager { get; set; }
        public double WindowSize { get; set; } = 100;

        const double MIN_SECOND = 0.1;
        Image<double> attentionMap;
        Image<Pixel> img;

        public void Init()
        {

        }

        public void BuildActivationMap(PointF position, SizeF size, TimeSpan duration, bool shouldRemove)
        {
            if (attentionMap != null)
            {

                ImageProxy<double> imageProxy = attentionMap?.Proxy(
                    new Rectangle((int)(position.X - size.Width / 2), (int)(position.Y - size.Height / 2),
                    (int)size.Width, (int)size.Height));

                var ts = duration;

                var removeFactor = shouldRemove ? -1 : 1;
                imageProxy?.ApplyFilter((px, pt) =>
                {
                    var ix = (pt.X / size.Width) * 2 - 1;
                    var iy = (pt.Y / size.Height) * 2 - 1;
                    if (ix * ix + iy * iy > 1)//out of circle = original pixel
                        return px;
                    var distance = Math.Pow((ix * ix + iy * iy), 0.5);

                    //=2*(1- 2^(D1*A1+C1) / (2^(D1*A1+C1)+1))
                    var _base = 1.5d;
                    var _mult_dist = 8;
                    var _off = -1;
                    var _mult = 2;
                    var distanceFactor = _mult * (1 - Math.Pow(_base, _mult_dist * distance + _off) / (Math.Pow(_base, _mult_dist * distance + _off) + 1));
                    return px += ts.TotalMilliseconds * distanceFactor * removeFactor;
                });
            }
        }



        public void Reset()
        {
            attentionMap?.ApplyFilter((px, pt) => 0);
        }
        public double Width => attentionMap?.Width ?? double.NaN;
        public double Height => attentionMap?.Height ?? double.NaN;
        public string Filename { get; private set; }
        public Image<Pixel> Image => img;

        private FileInfo[] files;

        public int ImageIndex { get; set; } = 0;
        public bool AllowWithoutLabel { get; internal set; }

        internal Image<Pixel> OpenImage(string fileName, bool loadFolder)
        {
            try
            {
                var file = new FileInfo(fileName);
                var dir = file.Directory;

                ConfigurationManager.LastOpenedDirectoryImage = dir.FullName;
                
                var totFileName = fileName;
                FileInfo fi = new FileInfo(totFileName);
                Filename = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);

                img = Program.LoadFile_<Pixel>(fileName);
              
                attentionMap = new Image<double>(img.Width, img.Height);
                attentionMap.ApplyFilter((px, pt) => 0);

                if (loadFolder)
                {
                    files = dir.GetFiles();
                    if (Program.verbose)
                        Console.WriteLine("files[0]={0}, file={1}, files.Length={2}", files[0], file, files.Length);
                    ImageIndex = -1;
                    for (var i = 0; i < files.Length; i++)
                        if (files[i].FullName == file.FullName)
                        {
                            ImageIndex = i;
                            break;
                        }
                    if (Program.verbose)
                        Console.WriteLine("Loading image {0}", ImageIndex);
                }

                return img;
            }
            catch
            {
                return new Image<Pixel>(1, 1);
            }
        }
        public string Next()
        {
            FileInfo fi = null;
            bool hasLoaded = false;
            while (!hasLoaded)
            {
                ImageIndex++;
                if (ImageIndex >= files.Length)
                    ImageIndex = 0;
                fi = files[ImageIndex];
                Filename = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
                hasLoaded = LoadCurrent();
            }
            if (Program.verbose)
                Console.WriteLine("Loading image {0}", ImageIndex);
            return fi.FullName;
        }
        public string FastNext()
        {
            ImageIndex++;
            if (ImageIndex >= files.Length)
                ImageIndex = 0;
            FileInfo fi = files[ImageIndex];
            Filename = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
            if (Program.verbose)
                Console.WriteLine("Loading image {0}", ImageIndex);
            return fi.FullName;
        }
        public bool LoadCurrent()
        {
            try
            {
                
                FileInfo fi = files[ImageIndex];
                img = Program.LoadFile_<Pixel>(fi.FullName);
             
                attentionMap = new Image<double>(img.Width, img.Height);
                attentionMap.ApplyFilter((px, pt) => 0);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public string Previous()
        {
            ImageIndex--;
            if (ImageIndex < 0)
                ImageIndex = files.Length - 1;
            FileInfo fi = files[ImageIndex];
            Filename = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
            img = Program.LoadFile_<Pixel>(fi.FullName);
           
            attentionMap = new Image<double>(img.Width, img.Height);
            attentionMap.ApplyFilter((px, pt) => 0);
            if (Program.verbose)
                Console.WriteLine("Loading image {0}", ImageIndex);
            return fi.FullName;
        }
        public void GenerateGrayscale(out Image<byte> grayscale)
        {
            var size = attentionMap.Width * attentionMap.Height;
            var max = Math.Min(5000, (from x in Enumerable.Range(0, size) select attentionMap[x]).Max());

            var normalized = attentionMap.Clone();
            normalized.ApplyFilter((px, pt) => Math.Max(0, px - MIN_SECOND * 1000));
            normalized.ApplyFilter((px, pt) => Math.Min(px, max) / max);
            Image<byte> hray = grayscale = normalized.ConvertUsing<byte>((dbl) => (byte)(dbl * 255));
            normalized.Dispose();
        }
        public void GenerateApplied(out Image<Pixel> applied, Image<byte> hray)
        {
            applied = img.Clone();
            applied.ApplyFilter((px, pt) =>
            {
                return Pixel.FromArgb(px, hray[pt]).Over(Pixel.FromArgb(255, 128, 128, 128));
            });
        }



        public void GenerateGrayscaleAndApplied(
          out Image<byte> grayscale,
          out Image<Pixel> applied)
        {
            GenerateGrayscale(out grayscale);
            GenerateApplied(out applied, grayscale);
        }


        public bool IsSet()
        {
            return attentionMap != null;
        }

        public Image<Pixel> GetImageCopy()
        {
            return img.Clone();
        }

    }
}
