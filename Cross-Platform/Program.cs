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
                Console.WriteLine("./DRAL [ --help | -h ] [--new | -n | --retag | -r] [ --no-window | --cli ] [ --fix-dataset | --fix | -f ]\r\n\t --help | -h : Display this help\r\n\t --no-window | --cli : Run in command line mode\r\n\t --fix-dataset | --fix | -f Run the fix dataset command\r\n\t --new | -n Start in tagger mode\r\n\t --retag | -r Start in retagger mode");
                return;
            }
            if (args.Contains("-v") || args.Contains("--verbose"))
                verbose = true;
            Gtk.Application.Init();

            NewTagWindow ntWindow = new NewTagWindow();
            RetagWindow rtWindow = new RetagWindow();
            DRALWindow window=null;
            
            if (args.Contains("--no-window") || args.Contains("--cli"))
                withWindow = false;
            if (args.Contains("--new") || args.Contains("-n"))
                window = ntWindow;
            else if (args.Contains("--retag") || args.Contains("-r") || window==null)
                window = rtWindow;

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
