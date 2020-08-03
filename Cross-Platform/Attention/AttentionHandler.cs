using AttentionAndRetag.Config;
using DRAL;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.IO;
using System.Linq;

namespace AttentionAndRetag.Attention
{
    /// <summary>
    /// Class for generating the attention from movement
    /// </summary>
    public class AttentionHandler
    {
        public ConfigurationManager? ConfigurationManager { get; set; }
        /// <summary>
        /// Minimum number of second of attention on a pixel otherwise 0
        /// </summary>
        private const double MIN_SECOND = 0.1;
        /// <summary>
        /// A step in building the attention map
        /// </summary>
        /// <param name="position">The location shown</param>
        /// <param name="size">The size of the visible part</param>
        /// <param name="duration">Duration at that point</param>
        /// <param name="shouldRemove">Is positive or negative change, for UNDO</param>
        public void BuildActivationMap(PointF position,
                                       SizeF size,
                                       TimeSpan duration,
                                       bool shouldRemove)
        {
            if (AttentionMap != null)
            {
                ImageProxy<double> imageProxy = AttentionMap?.Proxy(
                    new Rectangle((int)(position.X - size.Width / 2), (int)(position.Y - size.Height / 2),
                    (int)size.Width, (int)size.Height));
                //Apply a change to every pixel
                imageProxy?.ApplyFilter((value, location) =>
                {
                    var ix = (location.X / size.Width) * 2 - 1;
                    var iy = (location.Y / size.Height) * 2 - 1;
                    if (ix * ix + iy * iy > 1)//out of circle = original pixel
                        return value;
                    var distance = Math.Pow((ix * ix + iy * iy), 0.5);

                    //=2*(1- 1.5^(8*dist-1) / (1.5^(8*dist-1)+1))
                    const double _base = 1.5d;
                    const double _mult_dist = 8;
                    const double _off = -1;
                    const double _mult = 2;

                    var distanceFactor = _mult * (1 - Math.Pow(_base, _mult_dist * distance + _off) / (Math.Pow(_base, _mult_dist * distance + _off) + 1));

                    //If remove(subtract) multiply by minus one
                    var removeFactor = shouldRemove ? -1 : 1;
                    return value += duration.TotalMilliseconds * distanceFactor * removeFactor;
                });
            }
        }


        /// <summary>
        /// Reset the attention map to zero
        /// </summary>
        public void Reset() => AttentionMap?.ApplyFilter((px, pt) => 0);
        /// <summary>
        /// Current size of the attention map or NaN is no image is loaded
        /// </summary>
        public double Width => AttentionMap?.Width ?? double.NaN;
        /// <summary>
        /// Current size of the attention map or NaN is no image is loaded
        /// </summary>
        public double Height => AttentionMap?.Height ?? double.NaN;
        /// <summary>
        /// Current loaded file or null
        /// </summary>
        public string? Filename { get; private set; }
        /// <summary>
        /// Current loaded image, RGB
        /// </summary>
        public Image<Pixel>? Image { get; private set; }
        /// <summary>
        /// List of all files in the directory
        /// </summary>
        private FileInfo[]? files;
        /// <summary>
        /// Index of the currently used file
        /// </summary>
        public int ImageIndex { get; set; } = 0;
        /// <summary>
        /// A flag to allow or not image without label file, should only be true while in TAG mode
        /// </summary>
        public bool AllowWithoutLabel { get; internal set; }
        /// <summary>
        /// The attention map currently being built
        /// </summary>
        public Image<double>? AttentionMap { get; private set; }
        /// <summary>
        /// Load an image from the disk
        /// </summary>
        /// <param name="fileName">Image path</param>
        /// <param name="loadFolder">Load a list of image in the directory</param>
        /// <returns></returns>
        internal Image<Pixel> OpenImage(string fileName,
                                        bool loadFolder)
        {
            try
            {
                var file = new FileInfo(fileName);
                var dir = file.Directory;
                //Keep track
                ConfigurationManager.LastOpenedDirectoryImage = dir.FullName;

                var totFileName = fileName;
                var fileInfoTotal = new FileInfo(totFileName);
                Filename = fileInfoTotal.Name.Substring(0, fileInfoTotal.Name.Length - fileInfoTotal.Extension.Length);
                //Load the image using a standard loader
                Image = Program.LoadFile_<Pixel>(fileName);
                //Create an empty attention map
                AttentionMap = new Image<double>(Image.Width, Image.Height);
                AttentionMap.ApplyFilter((px, pt) => 0);

                if (loadFolder)
                {
                    //Create the list of image in the directory
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

                return Image;
            }
            catch
            {
                //Return a one pixel image in case the file was not processable
                return new Image<Pixel>(1, 1);
            }
        }
        /// <summary>
        /// Move to the next image, load it with it`s label
        /// </summary>
        /// <returns>Full image path</returns>
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
        /// <summary>
        /// Move to the next image, does load it with it`s label
        /// </summary>
        /// <returns>Full image path</returns>
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
        /// <summary>
        /// To be used after FastNext to load the image at the current index
        /// </summary>
        /// <returns>Success</returns>
        public bool LoadCurrent()
        {
            try
            {

                FileInfo fi = files[ImageIndex];
                Image = Program.LoadFile_<Pixel>(fi.FullName);
                //Create an empty attention map
                AttentionMap = new Image<double>(Image.Width, Image.Height);
                AttentionMap.ApplyFilter((px, pt) => 0);
                //The image did load
                return true;
            }
            catch
            {
                //The image did not load
                return false;
            }
        }
        /// <summary>
        /// Move to the previous image, does load it with it`s label
        /// </summary>
        /// <returns>Full image path</returns>
        public string Previous()
        {
            ImageIndex--;
            if (ImageIndex < 0)
                ImageIndex = files.Length - 1;
            FileInfo fi = files[ImageIndex];
            Filename = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
            Image = Program.LoadFile_<Pixel>(fi.FullName);

            AttentionMap = new Image<double>(Image.Width, Image.Height);
            AttentionMap.ApplyFilter((px, pt) => 0);
            if (Program.verbose)
                Console.WriteLine("Loading image {0}", ImageIndex);
            return fi.FullName;
        }
        /// <summary>
        /// Output the attention map in a grayscale image of bytes
        /// </summary>
        /// <param name="grayscale">The output</param>
        public void GenerateGrayscale(out Image<byte> grayscale)
        {
            var size = AttentionMap.Width * AttentionMap.Height;
            var max = Math.Min(5000, (from x in Enumerable.Range(0, size) select AttentionMap[x]).Max());

            var normalized = AttentionMap.Clone();
            normalized.ApplyFilter((px, pt) => Math.Max(0, px - MIN_SECOND * 1000));
            normalized.ApplyFilter((px, pt) => Math.Min(px, max) / max);
            Image<byte> hray = grayscale = normalized.ConvertUsing<byte>((dbl) => (byte)(dbl * 255));
            normalized.Dispose();
        }
        /// <summary>
        /// Output the attention map in a grayscale image of bytes
        /// </summary>
        public Image<byte> GenerateGrayscale()
        {
            GenerateGrayscale(out var img);
            return img;
        }
        /// <summary>
        /// Output the attention merged with the RGB
        /// </summary>
        /// <param name="applied">Output</param>
        /// <param name="attentionMap">Attention map</param>
        public void GenerateApplied(out Image<Pixel> applied, Image<byte> attentionMap)
        {
            applied = Image.Clone();
            applied.ApplyFilter((px, pt) =>
            {
                return Pixel.FromArgb(px, attentionMap[pt]).Over(Pixel.FromArgb(255, 128, 128, 128));
            });
        }
        /// <summary>
        /// Output the attention merged with the RGB
        /// </summary>
        /// /// <param name="attentionMap">Attention map</param>
        public Image<Pixel> GenerateApplied(Image<byte> attentionMap)
        {
            GenerateApplied(out var img, attentionMap);
            return img;
        }

        /// <summary>
        /// Generate both grayscale and applied in a single operation
        /// </summary>
        /// <param name="grayscale"></param>
        /// <param name="applied"></param>
        public void GenerateGrayscaleAndApplied(
          out Image<byte> grayscale,
          out Image<Pixel> applied)
        {
            grayscale = GenerateGrayscale();
            applied = GenerateApplied(grayscale);
        }
        /// <summary>
        /// Generate both grayscale and applied in a single operation
        /// </summary>
        public (Image<byte>, Image<Pixel>) GenerateGrayscaleAndApplied()
        {
            GenerateGrayscaleAndApplied(out var gray, out var img);
            return (gray, img);
        }

        /// <summary>
        /// Is there an image currently
        /// </summary>
        public bool IsSet => AttentionMap != null;
        /// <summary>
        /// Create a copy of the image
        /// </summary>
        public Image<Pixel> ImageCopy => Image.Clone();

    }
}
