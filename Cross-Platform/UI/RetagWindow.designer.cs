using DRAL.UI;
using Gtk;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.Text;

namespace DRAL.UI
{
    partial class RetagWindow
    {
        private CheckButton chkGenNow;
        private Label lblCount;
        private Window gtkWin;
        private FixedSizeImage iOri;
        private FixedSizeImage iActivated;
        private FixedSizeImage iActivation;
        private Fixed left;
        private FixedSizeImage pictureBox;
        private Box right;
        private DrawingArea rectangle_top;
        private DrawingArea rectangle_left;
        private DrawingArea rectangle_right;
        private DrawingArea rectangle_bottom;
        private DrawingArea viewCircle;
        internal List<Button> buttons = new List<Button>();
        public string Count
        {
            get
            {
                return lblCount.Text;
            }
            set
            {
                Application.Invoke(delegate { lblCount.Text = value; });
            }
        }
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

            //Wfrm = 13/9 of Wimg
            //Wimg = 9*Wfrm/13
            int maxImageWidth = 9 * size.Width / 13;
            int maxImageHeight = size.Height - 100;
            int maxImageWidthTakingImageHeight = maxImageHeight * 4 / 3;
            int maxImageHeightTakingImageWidth = maxImageWidth * 3 / 4;

            var imageWidth = Math.Min(maxImageWidth, maxImageWidthTakingImageHeight);
            var imageHeight = Math.Min(maxImageHeight, maxImageHeightTakingImageWidth);

            Box bx = new Box(Orientation.Vertical, 1);
            Box menu = new Box(Orientation.Horizontal, 1);
            Button buttonLoadLabels = new Button() { Label = "Load labels" };
            buttonLoadLabels.Clicked += ButtonLoadLabels_Clicked;
            menu.Add(buttonLoadLabels);
            buttons.Add(buttonLoadLabels);
            Button buttonLoadImages = new Button() { Label = "Load Image" };
            buttonLoadImages.Clicked += ButtonLoadImages_Clicked;
            menu.Add(buttonLoadImages);
            buttons.Add(buttonLoadImages);
            Button buttonSave = new Button() { Label = "Save" };
            buttonSave.Clicked += ButtonSave_Clicked;
            menu.Add(buttonSave);
            buttons.Add(buttonSave);
            Button btnFixMissing = new Button() { Label = "Repair Dataset" };
            btnFixMissing.Clicked += BtnFixMissing_Clicked;
            menu.Add(btnFixMissing);
            buttons.Add(btnFixMissing);
            chkGenNow = new CheckButton("Generate now");
            chkGenNow.Active = true;
            menu.Add(chkGenNow);

            lblCount = new Label("0");
            menu.Add(lblCount);
            Label lblImageInTraining = new Label("images in training");
            menu.Add(lblImageInTraining);
            
            Box bottom = new Box(Orientation.Horizontal, 1);
            EventBox evt = new EventBox();
            left = new Fixed();
            evt.Add(left);

            pictureBox = new FixedSizeImage(new MoyskleyTech.ImageProcessing.Image.Size(imageWidth,imageHeight));
            pictureBox.WidthRequest = imageWidth;
            left.Put(pictureBox, 0,0);

            evt.AddEvents((int)Gdk.EventMask.AllEventsMask);
            evt.MotionNotifyEvent += Left_MotionNotifyEvent;
            evt.ButtonPressEvent += Evt_ButtonPressEvent;
            evt.ButtonReleaseEvent += Evt_ButtonReleaseEvent;

            right = new Box(Orientation.Vertical, 1);
            right.WidthRequest = 200;
            var siz = new MoyskleyTech.ImageProcessing.Image.Size(imageWidth * 4/9, imageHeight/3);
            iOri = new FixedSizeImage(siz);
            iActivated = new FixedSizeImage(siz);
            iActivation = new FixedSizeImage(siz);
            right.Add(iOri);
            right.Add(iActivated);
            right.Add(iActivation);
          
            rectangle_top = new DrawingArea();
            rectangle_left = new DrawingArea();
            rectangle_right = new DrawingArea();
            rectangle_bottom = new DrawingArea();

            rectangle_top.Drawn += Black_Drawn;
            rectangle_left.Drawn += Black_Drawn;
            rectangle_right.Drawn += Black_Drawn;
            rectangle_bottom.Drawn += Black_Drawn;

            viewCircle = new DrawingArea();
            viewCircle.WidthRequest = windowSize;
            viewCircle.HeightRequest = windowSize;
            viewCircle.Drawn += Da_Drawn;

            left.Put(rectangle_top, 0, 0);
            left.Put(rectangle_left, 0, 0);
            left.Put(rectangle_right, 0, 0);
            left.Put(rectangle_bottom, 0, 0);
            left.Put(viewCircle, 0, 0);

            bottom.Add(evt);
            bottom.Add(right);

            menu.HeightRequest = 30;
            bx.Add(menu);
            bx.Add(bottom);
            gtkWin.KeyPressEvent += GtkWin_KeyPressEvent;
            gtkWin.Add(bx);
            //Show Everything
            gtkWin.DeleteEvent += delegate { Application.Quit(); };
        }

     

        private void Black_Drawn(object o, DrawnArgs args)
        {
            var da= (DrawingArea)o;

            var ctx = args.Cr;
            ctx.SetSourceRGB(0, 0, 0);
            ctx.Rectangle(0, 0, da.AllocatedWidth, da.AllocatedHeight);
            ctx.Fill();
        }

    }
}
