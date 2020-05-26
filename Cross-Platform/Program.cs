using System;
using System.Threading.Tasks;
using DRAL.UI;
using Gtk;

namespace DRAL
{
    class Program
    {
        static void Main(string[] args)
        {
            Gtk.Application.Init();

            RetagWindow window = new RetagWindow();
            window.Show();

            Application.Run();
        }
    }
}
