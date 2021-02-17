using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using AttentionAndRetag.Retag;
using Cairo;
using DRAL.UI;
using Gtk;
using ImageProcessing.JPEGCodec;
using MoyskleyTech.ImageProcessing.Image;

namespace DRAL
{
    class Program
    {
        internal static bool withWindow = true;
        internal static bool autoClose = false;
        internal static bool verbose = false;
        internal static string? file_in = null;
        internal static string? file_out = null;
        internal static int skip = 0;
        internal static int take = int.MaxValue;
        static void Main(string[] args)
        {
            Console.WriteLine("DRAL V." + Assembly.GetEntryAssembly().GetName().Version);
            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("./DRAL [ --help | -h ] [--new | -n | --retag | -r | --show | -s ] [ --no-window | --cli ] [ --fix-dataset | --fix | -f ]");
                Console.WriteLine("     -f, --fix, --fix-dataset");
                Console.WriteLine("         Run the fix dataset command");
                Console.WriteLine("     -h, --help");
                Console.WriteLine("         Display this help");
                Console.WriteLine("     -n, --new");
                Console.WriteLine("         Start in tagger mode");
                Console.WriteLine("     --no-window, --cli ");
                Console.WriteLine("         Run in command line mode");
                Console.WriteLine("     -r, --retag");
                Console.WriteLine("         Start in retagger mode");
                Console.WriteLine("     -s, --show");
                Console.WriteLine("         Start in display mode");
                Console.WriteLine("         --in {file}, -i {file}");
                Console.WriteLine("             [mandatory]Input file for the display, imply --cli");
                Console.WriteLine("         --out {file}, -o {file}");
                Console.WriteLine("             Output file for the display, imply --cli");
                Console.WriteLine("     --skip {n}");
                Console.WriteLine("         Skip 'n' file(s) while fixing");
                Console.WriteLine("     --take {n}");
                Console.WriteLine("         Take 'n' file(s) while fixing");
                Console.WriteLine("     --verbose, -v");
                Console.WriteLine("         Activate verbose mode");

                return;
            }
            if (args.Contains("-v") || args.Contains("--verbose"))
                verbose = true;
            Gtk.Application.Init();

            if (args.Contains("--out"))
            {
                var outIdx = Array.IndexOf(args, "--out");
                file_out = args[outIdx + 1];
            }
            if (args.Contains("-o"))
            {
                var outIdx = Array.IndexOf(args, "-o");
                file_out = args[outIdx + 1];
            }
            if (args.Contains("--in"))
            {
                var inIdx = Array.IndexOf(args, "--in");
                file_in = args[inIdx + 1];
            }
            if (args.Contains("-i"))
            {
                var inIdx = Array.IndexOf(args, "-i");
                file_in = args[inIdx + 1];
            }
            if (args.Contains("--skip"))
            {
                var skipIdx = Array.IndexOf(args, "--skip");
                skip = int.Parse(args[skipIdx + 1]);
            }
            if (args.Contains("--take"))
            {
                var takeIdx = Array.IndexOf(args, "--take");
                take = int.Parse(args[takeIdx + 1]);
            }

            DRALWindow window = new ErrorWindow();

            if (args.Contains("--no-window") || args.Contains("--cli") || ((args.Contains("-s") || args.Contains("--show")) && (args.Contains("--out") || args.Contains("-o"))))
                withWindow = false;
            if (args.Contains("--new") || args.Contains("-n"))
                window = new NewTagWindow();
            if (args.Contains("--show") || args.Contains("-s"))
                window = new ShowWindow();
            if ( args.Contains("--convert") || args.Contains("-c") )
                window = new ConvertWindow();
            if (args.Contains("--retag") || args.Contains("-r") || window == null)
                window = new RetagWindow();
            if(withWindow && window is ErrorWindow)
            {
                Console.Error.WriteLine("missing action argument");
                return;
            }

            if (withWindow)
                window.Show();
            if (args.Contains("--fix") || args.Contains("-f") || args.Contains("--fix-dataset") ||
               (window is ShowWindow && (args.Contains("--out") || args.Contains("-o"))))
            {
                autoClose = true;
                _=Task.Run(async () =>
                {
                    await Task.Delay(500);
                    await window.Fix();
                    await Task.Delay(3000);
                    Application.Quit();
                });
                Application.Run();
            }
            if (withWindow && !autoClose)
                Application.Run();
        }

        internal static IMAGE_LABEL_INFO LoadLabelEnd(string full_file_name_label)
        {
            try
            {
                IMAGE_LABEL_INFO ret = new IMAGE_LABEL_INFO
                {
                    labels = new System.Collections.Generic.List<LABEL>()
                };
                var lines = System.IO.File.ReadAllLines(full_file_name_label);
                foreach (var line in lines)
                {
                    var spl = line.Split(" ");
                    LABEL lbl = new LABEL
                    {
                        category = spl[0],
                        box2d = new BOX2D()
                        {
                            x1 = double.Parse(spl[1]),
                            y1 = double.Parse(spl[2]),
                            x2 = double.Parse(spl[1]) + double.Parse(spl[3]),
                            y2 = double.Parse(spl[2]) + double.Parse(spl[4])
                        }
                    };
                    ret.labels.Add(lbl);
                }
                return ret;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message + e.StackTrace);
                return new IMAGE_LABEL_INFO();
            }
        }

        internal static Image<T> LoadFile_<T>(string p)
         where T : unmanaged
        {
            try
            {
                BitmapFactory bf = new BitmapFactory();
                using var fs = System.IO.File.OpenRead(p);
                return bf.Decode(fs).ConvertTo<T>();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message + e.StackTrace);
                return new Image<T>(1, 1);
            }
        }
        internal static bool SaveFile_<T>(string _name_, Image<T>? img)
             where T : unmanaged
        {
            try
            {
                using var s = System.IO.File.Create(_name_);
                new JPEGCodec().Save<T>(img, s);
                return true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message + e.StackTrace);
                return false;
            }
        }
        internal static string TS(double v)
        {
            return v.ToString(new CultureInfo("en-US"));
        }
        internal static void SaveLabel(string v,
                                       IMAGE_LABEL_INFO label,
                                       double w,
                                       double h)
        {
            try
            {
                System.IO.File.WriteAllText(v, label.GenerateFile(w, h));
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message + e.StackTrace);
            }
        }
    }
}
