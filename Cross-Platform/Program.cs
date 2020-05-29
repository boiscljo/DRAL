using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DRAL.UI;
using Gtk;

namespace DRAL
{
    class Program
    {
        internal static bool withWindow = true;
        internal static bool autoClose = false;
        internal static bool verbose = false;
        static void Main(string[] args)
        {
            Console.WriteLine("DRAL V." + Assembly.GetEntryAssembly().GetName().Version);
            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("./DRAL [ --help | -h ] [ --no-window | --cli ] [ --fix-dataset | --fix | -f ]\r\n\t --help | -h : Display this help\r\n\t --no-window | --cli : Run in command line mode\r\n\t --fix-dataset | --fix | -f Run the fix dataset command");
                return;
            }
            if (args.Contains("-v") || args.Contains("--verbose"))
                verbose = true;
            Gtk.Application.Init();

            RetagWindow window = new RetagWindow();
           
            if (args.Contains("--no-window") || args.Contains("--cli"))
                withWindow = false;
            if (withWindow)
                window.Show();
            if (args.Contains("--fix") || args.Contains("-f") || args.Contains("--fix-dataset"))
            {
                autoClose = true;
                Task.Run(async ()=>{ await Task.Delay(500); await window.Fix(); await Task.Delay(3000); Application.Quit(); });
                Application.Run();
            }
            if (withWindow && !autoClose)
                Application.Run();
        }
    }
}
