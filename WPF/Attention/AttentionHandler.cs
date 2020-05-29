using AttentionAndRetag.Retag;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using AttentionAndRetag.Config;
namespace AttentionAndRetag.Attention
{
    public class AttentionHandler
    {
        public ConfigurationManager ConfigurationManager { get; set; }
        public double WindowSize { get; set; } = 100;

        double minSecond = 0.1;
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
                    new Rectangle((int)(position.X-size.Width/2), (int)(position.Y-size.Height / 2),
                    (int)size.Width, (int)size.Height));

                var ts = duration;
                
                var removeFactor = shouldRemove ? -1 : 1;
                imageProxy?.ApplyFilter((px, pt) =>
                {
                    var ix = (pt.X / size.Width) * 2 - 1;
                    var iy = (pt.Y / size.Height) * 2 - 1;
                    if (ix * ix + iy * iy > 1)//out of circle = original pixel
                        return px;
                    var distanceFactor = 1 - (ix * ix + iy * iy);

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
        internal Image<Pixel> OpenImage(string fileName, bool loadFolder)
        {
            var file = new FileInfo(fileName);
            var dir = file.Directory;

            ConfigurationManager.LastOpenedDirectoryImage = dir.FullName;
            var factory = new BitmapFactory();
            var totFileName = fileName;
            FileInfo fi = new FileInfo(totFileName);
            Filename = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);

            using (var s = System.IO.File.OpenRead(fileName))
                img = factory.Decode(s);

            attentionMap = new Image<double>(img.Width, img.Height);
            attentionMap.ApplyFilter((px, pt) => 0);

            if (loadFolder)
            {
                files = dir.GetFiles();
                ImageIndex = Array.IndexOf(files,file);
            }

            return img;
        }
        public string Next()
        {
            ImageIndex++;
            if (ImageIndex > files.Length)
                ImageIndex = 0;
            FileInfo fi = files[ImageIndex];
            Filename = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
            LoadCurrent();

            return fi.FullName;
        }
        public string FastNext()
        {
            ImageIndex++;
            if (ImageIndex > files.Length)
                ImageIndex = 0;
            FileInfo fi = files[ImageIndex];
            Filename = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);

            return fi.FullName;
        }
        public void LoadCurrent()
        {
            var factory = new BitmapFactory();
            FileInfo fi = files[ImageIndex];
            using (var s = System.IO.File.OpenRead(fi.FullName))
                img = factory.Decode(s);

            attentionMap = new Image<double>(img.Width, img.Height);
            attentionMap.ApplyFilter((px, pt) => 0);
        }
        public string Previous()
        {
            ImageIndex--;
            if (ImageIndex <0)
                ImageIndex = files.Length-1;
            FileInfo fi = files[ImageIndex];
            Filename = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
            var factory = new BitmapFactory();
            using (var s = System.IO.File.OpenRead(fi.FullName))
                img = factory.Decode(s);

            attentionMap = new Image<double>(img.Width, img.Height);
            attentionMap.ApplyFilter((px, pt) => 0);

            return fi.FullName;
        }
        public void GenerateGrayscale(out Image<byte> grayscale)
        {
            var size = attentionMap.Width * attentionMap.Height;
            var max = Math.Min(5000, (from x in Enumerable.Range(0, size) select attentionMap[x]).Max());

            var normalized = attentionMap.Clone();
            normalized.ApplyFilter((px, pt) => Math.Max(0, px - minSecond * 1000));
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
