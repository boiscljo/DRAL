using DRAL.UI;
using Gtk;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.Text;

namespace DRAL.UI
{
    partial class TagWindow
    {
        private Window gtkWin;
        private DrawingArea imgBox;
        internal List<Button> buttons = new List<Button>();
      
        public void Init(string image)
        {
            gtkWin = new Window("DRAL tagger["+image+"]");

            var size = gtkWin.Display.PrimaryMonitor.Workarea.Size;

            Image<Pixel> icon = new Image<Pixel>(32, 32);
            icon.ApplyFilter((_, _2) => Pixels.Black);
            Graphics.FromImage(icon).DrawString("DR", new FontSize(BaseFonts.Premia, 2), Pixels.Red, 0, 0);
            Graphics.FromImage(icon).DrawString("AL", new FontSize(BaseFonts.Premia, 2), Pixels.Red, 0, 16);
            gtkWin.Icon = GtkWrapper.ToPixbuf(icon);
            gtkWin.Resize(size.Width, size.Height);

            int maxImageWidth = size.Width -100;
            int maxImageHeight = size.Height - 100;
            int maxImageWidthTakingImageHeight = maxImageHeight * 4 / 3;
            int maxImageHeightTakingImageWidth = maxImageWidth * 3 / 4;

            var imageWidth = Math.Min(maxImageWidth, maxImageWidthTakingImageHeight);
            var imageHeight = Math.Min(maxImageHeight, maxImageHeightTakingImageWidth);

            Box bx = new Box(Orientation.Vertical, 1);
            Box menu = new Box(Orientation.Horizontal, 1);

          
            Button buttonSave = new Button() { Label = "Save" };
            buttonSave.Clicked += ButtonSave_Clicked;
            menu.Add(buttonSave);
            buttons.Add(buttonSave);
            Button btnCancel = new Button() { Label = "Cancel" };
            btnCancel.Clicked += BtnCancel_Clicked;
            menu.Add(btnCancel);
            buttons.Add(btnCancel);
          
            EventBox evt = new EventBox();
            imgBox = new DrawingArea();
            evt.Add(imgBox);

            imgBox.WidthRequest = imageWidth;
            imgBox.HeightRequest = imageHeight;
            imgBox.Drawn += ImgBox_Drawn;

            evt.AddEvents((int)Gdk.EventMask.AllEventsMask);
            evt.MotionNotifyEvent += ImgBox_MotionNotifyEvent;
            evt.ButtonPressEvent += Evt_ButtonPressEvent;
            evt.ButtonReleaseEvent += Evt_ButtonReleaseEvent;

           
            menu.HeightRequest = 30;
            bx.Add(evt);
            bx.Add(menu);

            gtkWin.KeyPressEvent += GtkWin_KeyPressEvent;
            gtkWin.Add(bx);

            gtkWin.DeleteEvent += GtkWin_DeleteEvent;

        }

        private void GtkWin_DeleteEvent(object o, DeleteEventArgs args)
        {
            result.TrySetResult(false);
        }
    }
}
