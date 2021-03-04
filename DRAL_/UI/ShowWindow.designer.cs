using DRAL.UI;
using Gtk;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DRAL.UI
{
    partial class ShowWindow
    {
        Window gtkWin;
        Gtk.Image img;
        TaskCompletionSource<object> task = new TaskCompletionSource<object>();
        public void Init()
        {
            gtkWin = new Window("DRAL Retag");

            var size = gtkWin.Display.PrimaryMonitor.Workarea.Size;

            Image<Pixel> icon = new Image<Pixel>(32, 32);
            icon.ApplyFilter((_, _2) => Pixels.Black);
            Graphics.FromImage(icon).DrawString("DR", new FontSize(BaseFonts.Premia, 2), Pixels.Red, 0, 0);
            Graphics.FromImage(icon).DrawString("AL", new FontSize(BaseFonts.Premia, 2), Pixels.Red, 0, 16);
            gtkWin.Icon = GtkWrapper.ToPixbuf(icon);
            gtkWin.Resize(size.Width, size.Height);

            img = new Gtk.Image();

            gtkWin.Add(img);
         
            //Show Everything
            gtkWin.DeleteEvent += delegate { task.TrySetResult(null); Application.Quit(); };
        }

   

    }
}
