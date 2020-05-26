using AttentionAndRetag.Attention;
using AttentionAndRetag.Config;
using AttentionAndRetag.Model;
using AttentionAndRetag.Retag;
using Cairo;
using DRAL.UI;
using Gtk;
using ImageProcessing.JPEGCodec;
using ImageProcessing.PNGCodec;
using Microsoft.VisualBasic;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Rectangle = MoyskleyTech.ImageProcessing.Image.Rectangle;

namespace DRAL.UI
{
    public partial class RetagWindow
    {
        AttentionHandler attentionHandler;
        ConfigurationManager manager;
        Retagger retag;
        AttentionMapAnaliser analyser;
        PointF last;
        DisplayModel model;
        const int windowSize = 100;
        DateTime lastUpdate;
        private double dispBeginX;
        private double dispEndX;
        private double dispBeginY;
        private double dispEndY;

        public RetagWindow()
        {
            Init();
            PngCodec.Register();
            JPEGCodec.Register();

            manager = new ConfigurationManager();
            attentionHandler = new AttentionHandler() { ConfigurationManager = manager };
            retag = new Retagger() { ConfigurationManager = manager };
            analyser = new AttentionMapAnaliser() { ConfigurationManager = manager };

            manager.Init();
            analyser.Init();
            attentionHandler.Init();
            retag.Init();
            model = new DisplayModel(this);

            last = new PointF(-windowSize, -windowSize);
        }
        public void Show()
        {
            gtkWin.ShowAll();
        }
        private async void ButtonSave_Clicked(object sender, EventArgs e)
        {
            await GenerateAndSave();
        }

        private void ButtonLoadImages_Clicked(object sender, EventArgs e)
        {
            OpenImage();
        }

        private void ButtonLoadLabels_Clicked(object sender, EventArgs e)
        {
            Gtk.FileChooserDialog fc =
                new Gtk.FileChooserDialog("Load labels",
            gtkWin,
            Gtk.FileChooserAction.Open,
            "Cancel", Gtk.ResponseType.Cancel,
            "Open", Gtk.ResponseType.Accept);
            fc.Filter = new FileFilter();
            fc.Filter.AddPattern("*.json");

            if (fc.Run() == (int)Gtk.ResponseType.Accept)
            {
                manager.LoadLabels(fc.Filename);
                manager.SaveConfig();
            }
            //Destroy() to close the File Dialog
            fc.Dispose();
        }

        private async void GtkWin_KeyPressEvent(object o, KeyPressEventArgs args)
        {
            var e = args.Event.Key;
            if (e == Gdk.Key.r && args.Event.State == Gdk.ModifierType.ControlMask)
            {
                attentionHandler.Reset();
            }
            if (e == Gdk.Key.o && args.Event.State == Gdk.ModifierType.ControlMask)
            {
                OpenImage();
            }
            if (e == Gdk.Key.s && args.Event.State == Gdk.ModifierType.ControlMask)
            {
                await GenerateAndSave();
            }
            if (e == Gdk.Key.z && args.Event.State == Gdk.ModifierType.ControlMask)
            {
                Previous(true);
            }
            if (e == Gdk.Key.x && args.Event.State == Gdk.ModifierType.ControlMask)
            {
                Previous(false);
            }
            if (e == Gdk.Key.n && args.Event.State == Gdk.ModifierType.ControlMask)
            {
                if (await GenerateAndSave())
                    Next();
            }
            if ((e == Gdk.Key.y && args.Event.State == Gdk.ModifierType.ControlMask) ||
                (e == Gdk.Key.b && args.Event.State == Gdk.ModifierType.ControlMask))
            {
                Next();
            }
            if (e == Gdk.Key.w && args.Event.State == Gdk.ModifierType.ControlMask)
            {
                NextUntilNotRecorded();
            }
        }
        private void OpenImage()
        {

            Gtk.FileChooserDialog fc =
                new Gtk.FileChooserDialog("Load image",
            gtkWin,
            Gtk.FileChooserAction.Open,
            "Cancel", Gtk.ResponseType.Cancel,
            "Open", Gtk.ResponseType.Accept);
            fc.Filter = new FileFilter();
            fc.Filter.AddPattern("*.jpg");

            if (fc.Run() == (int)Gtk.ResponseType.Accept)
            {
                attentionHandler.OpenImage(fc.Filename, true);
                manager.SaveConfig();
                LoadImageInformation(fc.Filename);
            }
            
            //Destroy() to close the File Dialog
            fc.Dispose();
        }
        private void SaveFile_(string _name_, Image<Pixel> img)
        {
            using (var s = System.IO.File.Create(_name_))
            {
                new JPEGCodec().Save<Pixel>(img, s);
            }
        }
        private void SaveLabel(string v, IMAGE_LABEL_INFO label, double w, double h)
        {
            System.IO.File.WriteAllText(v, retag.GenerateFile(label, w, h));
        }
        private async Task<bool> GenerateAndSave()
        {
            bool ret = false;
            await Task.Yield();
            if (await model.RequestRun())
            {
                try
                {

                    if (attentionHandler.IsSet())
                    {
                        Image<byte> grayscale = null;
                        Image<Pixel> applied = null;
                        var _name_ = attentionHandler.Filename;
                        await Task.Run(() => attentionHandler.GenerateGrayscaleAndApplied(out grayscale, out applied));
                        var label = manager.GetLabel(attentionHandler.Filename);

                        Directory.CreateDirectory("./data");
                        Directory.CreateDirectory("./data/ori/images");
                        Directory.CreateDirectory("./data/ori/labels");
                        Directory.CreateDirectory("./data/imp/images");
                        Directory.CreateDirectory("./data/imp/labels");

                        PresentResult pr = new PresentResult();
                        
                        pr.iActivated.Image = applied;
                        pr.iOri.Image = attentionHandler.Image;
                        pr.iActivation.Image = grayscale.ConvertTo<Pixel>();

                        if ((ret = await pr.ShowDialogAsync()))
                        {
                            var newLabel = await retag.ImproveLabel(iActivated, attentionHandler.Image, grayscale, label);

                            SaveFile_("./data/ori/images/" + _name_ + ".jpg", attentionHandler.Image);
                            SaveLabel("./data/ori/labels/" + _name_ + ".txt", label, attentionHandler.Image.Width, attentionHandler.Image.Height);
                            SaveFile_("./data/imp/images/" + _name_ + ".jpg", applied);
                            SaveLabel("./data/imp/labels/" + _name_ + ".txt", newLabel, attentionHandler.Image.Width, attentionHandler.Image.Height);
                            model.HadChangedTraining();
                        }

                    }
                }
                catch (Exception e)
                {
                    MessageBox.ShowError(gtkWin, e.Message);
                }
                model.EndRun();
            }
            return ret;
        }
        private void Left_MotionNotifyEvent(object o, MotionNotifyEventArgs args)
        {
            var pos = new PointF(args.Event.X, args.Event.Y);

            DisplayMaskOver(new PointF(pos.X, pos.Y));//Display mask
            if (attentionHandler.IsSet())
            {
                var scaleX = (attentionHandler.Width / pictureBox.WidthRequest);
                var scaleY = (attentionHandler.Height / pictureBox.HeightRequest);
                var winSizeXImg = scaleX * windowSize;
                var winSizeYImg = scaleY * windowSize;

                var now = DateTime.Now;
                attentionHandler.BuildActivationMap(last, new SizeF(winSizeXImg, winSizeYImg), now - lastUpdate, false);//For previous position
                lastUpdate = now;

                var posOnImage = pos;
                last = new PointF(posOnImage.X * scaleX, posOnImage.Y * scaleY);

                /*model.RequestImageIn(iActivation, () =>
                {
                    attentionHandler.GenerateGrayscale(out Image<byte> gray);
                    return gray;
                });*/
            }
        }

        private void Previous(bool v)
        {
            attentionHandler.Previous();
            var lbl = manager.GetLabel(attentionHandler.Filename);
            while (lbl == null)
            {
                attentionHandler.Previous();
            }
            LoadImageInformation(attentionHandler.Filename);
            if (v)
            {
                var _name_ = attentionHandler.Filename;
                DeleteIfExists("./data/ori/images/" + _name_ + ".jpg");
                DeleteIfExists("./data/ori/labels/" + _name_ + ".txt");
                DeleteIfExists("./data/imp/images/" + _name_ + ".jpg");
                DeleteIfExists("./data/imp/labels/" + _name_ + ".txt");
            }
        }

        private void DeleteIfExists(string v)
        {
            if (System.IO.File.Exists(v))
                System.IO.File.Delete(v);
        }

        private void Next()
        {
            attentionHandler.Next();

            var lbl = manager.GetLabel(attentionHandler.Filename);
            while (lbl == null)
            {
                attentionHandler.Next();
                lbl = manager.GetLabel(attentionHandler.Filename);
            }
            LoadImageInformation(attentionHandler.Filename);
        }
        private void NextUntilNotRecorded()
        {
            while (ExistsInTraining())
            {
                attentionHandler.FastNext();

                var lbl = manager.GetLabel(attentionHandler.Filename);
                while (lbl == null)
                {
                    attentionHandler.FastNext();
                    lbl = manager.GetLabel(attentionHandler.Filename);
                }
            }
            attentionHandler.LoadCurrent();
            LoadImageInformation(attentionHandler.Filename);
        }

        private bool ExistsInTraining()
        {
            var _name_ = attentionHandler.Filename;
            var img_path = "./data/imp/images/" + _name_ + ".jpg";
            return (System.IO.File.Exists(img_path));
        }
        private void LoadImageInformation(string FileName)
        {
            var _name_ = attentionHandler.Filename;
            var lbl = manager.GetLabel(attentionHandler.Filename);
            if (lbl == null)
            {
                MessageBox.Show(gtkWin,"Could not tag an image without label, choose another");
            }
            else
            {
                gtkWin.Title = lbl.name;
                lastUpdate = DateTime.Now;
                iOri.Image = (attentionHandler.Image);
                pictureBox.Image = (attentionHandler.Image);
            }
            var img_path = "./data/imp/images/" + _name_ + ".jpg";
            var exists = (System.IO.File.Exists(img_path));
            if (exists)
            {
                BitmapFactory bitmapFactory = new BitmapFactory();
                using (var fs = System.IO.File.OpenRead(img_path))
                    iActivation.Image = (bitmapFactory.Decode(fs));
            }
            else
                iActivation.Image = null;
        }
        private void DisplayMaskOver(PointF location)
        {
            dispBeginX = location.X - windowSize / 2;
            dispEndX = location.X + windowSize / 2;
            dispBeginY = location.Y - windowSize / 2;
            dispEndY = location.Y + windowSize / 2;
            dispBeginX = Math.Min((int)dispBeginX, pictureBox.WidthRequest);
            dispBeginY = Math.Min((int)dispBeginY, pictureBox.HeightRequest);
            left.Move(rectangle_top, 0, 0);
            rectangle_top.HeightRequest = (int)Math.Max(dispBeginY, 0);
            rectangle_top.WidthRequest = pictureBox.WidthRequest;

            left.Move(rectangle_left, 0, 0);
            rectangle_left.HeightRequest = pictureBox.HeightRequest;
            rectangle_left.WidthRequest = (int)Math.Max(dispBeginX, 0);

            left.Move(rectangle_bottom, 0, Math.Min((int)dispEndY,pictureBox.HeightRequest));
            rectangle_bottom.HeightRequest = Math.Max(0,(int)(pictureBox.HeightRequest - dispEndY));
            rectangle_bottom.WidthRequest = pictureBox.WidthRequest;

            left.Move(rectangle_right, Math.Min((int)dispEndX, pictureBox.WidthRequest), 0);
            rectangle_right.HeightRequest = pictureBox.HeightRequest;
            rectangle_right.WidthRequest = Math.Max(0, (int)(pictureBox.WidthRequest - dispEndX));

            left.Move(viewCircle, (int)dispBeginX, (int)dispBeginY);
            viewCircle.WidthRequest = Math.Max(0, (int)(pictureBox.WidthRequest - dispBeginX));
            viewCircle.HeightRequest = Math.Max(0, (int)(pictureBox.HeightRequest - dispBeginY));
        }
        private void Da_Drawn(object o, DrawnArgs args)
        {
            if (attentionHandler.Image != null)
            {
                var scaleX = (attentionHandler.Width / pictureBox.WidthRequest);
                var scaleY = (attentionHandler.Height / pictureBox.HeightRequest);
                var winSizeXImg = scaleX * windowSize;
                var winSizeYImg = scaleY * windowSize;

                var SubImage = attentionHandler.Image[new Rectangle((int)(dispBeginX* scaleX), (int)(dispBeginY* scaleY), (int)(windowSize* scaleX), (int)(windowSize* scaleY))].ToImage<BGRA>();
                var img = SubImage.Rescale(windowSize, windowSize, ScalingMode.AverageInterpolate);
                ImageSurface ims = new ImageSurface(img.DataPointer, Format.Argb32, img.Width, img.Height, img.Width * 4);
                DrawingArea da = (DrawingArea)o;
                var ctx = args.Cr;
                ctx.SetSourceRGB(0, 0, 0);
                ctx.Rectangle(0, 0, windowSize, windowSize);
                ctx.Fill();
                ctx.SetSource(ims);
                ctx.Arc(windowSize / 2, windowSize / 2, windowSize / 2, 0, 2 * Math.PI);
                ctx.Fill();
                img.Dispose();
                SubImage.Dispose();
                ims.Dispose();
            }
        }
    }
}
