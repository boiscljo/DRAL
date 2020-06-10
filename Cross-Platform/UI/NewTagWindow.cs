using AttentionAndRetag.Attention;
using AttentionAndRetag.Config;
using AttentionAndRetag.Model;
using AttentionAndRetag.Retag;
using Cairo;
using DRAL.Tag;
using DRAL.UI;
using Gtk;
using ImageProcessing.JPEGCodec;
using ImageProcessing.PNGCodec;
using Microsoft.VisualBasic;
using MoyskleyTech.ImageProcessing;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Rectangle = MoyskleyTech.ImageProcessing.Image.Rectangle;

namespace DRAL.UI
{
    public partial class NewTagWindow : DRALWindow
    {
        AttentionHandler attentionHandler;
        ConfigurationManager manager;
        Tagger tagger;
        AttentionMapAnaliser analyser;
        PointF last;
        DisplayModelNew model;
        const int windowSize = 100;
        DateTime lastUpdate;
        private double dispBeginX;
        private double dispEndX;
        private double dispBeginY;
        private double dispEndY;

        Dictionary<uint, bool> buttonsPressed = new Dictionary<uint, bool>();

        public NewTagWindow()
        {
            CreateDirectories();

            Init();
            PngCodec.Register();
            JPEGCodec.Register();

            manager = new ConfigurationManager() { NeedLabel = false };
            attentionHandler = new AttentionHandler() { ConfigurationManager = manager, AllowWithoutLabel = true };
            tagger = new Tagger() { ConfigurationManager = manager };
            analyser = new AttentionMapAnaliser() { ConfigurationManager = manager };

            manager.Init();
            analyser.Init();
            attentionHandler.Init();
            tagger.Init();
            model = new DisplayModelNew(this);

            last = new PointF(-windowSize, -windowSize);
            buttonsPressed[1] = false;
            buttonsPressed[2] = false;
            buttonsPressed[3] = false;
        }
        public override void Show()
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

        private async void GtkWin_KeyPressEvent(object o, KeyPressEventArgs args)
        {
            var e = args.Event.Key;
            //Console.WriteLine(e.ToString() + args.Event.State.ToString());
            if ((e == Gdk.Key.r || e == Gdk.Key.R) && args.Event.State.HasFlag(Gdk.ModifierType.ControlMask))
            {
                attentionHandler.Reset();
            }
            if ((e == Gdk.Key.o || e == Gdk.Key.O) && args.Event.State.HasFlag(Gdk.ModifierType.ControlMask))
            {
                OpenImage();
            }
            if ((e == Gdk.Key.z || e == Gdk.Key.Z) && args.Event.State.HasFlag(Gdk.ModifierType.ControlMask))
            {
                Previous(true);
            }
            if ((e == Gdk.Key.x || e == Gdk.Key.X) && args.Event.State.HasFlag(Gdk.ModifierType.ControlMask))
            {
                Previous(false);
            }
            if ((e == Gdk.Key.n || e == Gdk.Key.s || e == Gdk.Key.N || e == Gdk.Key.S) && args.Event.State.HasFlag(Gdk.ModifierType.ControlMask))
            {
                if (await GenerateAndSave())
                    Next();
            }
            if ((e == Gdk.Key.y || e == Gdk.Key.b || e == Gdk.Key.Y || e == Gdk.Key.B) && args.Event.State.HasFlag(Gdk.ModifierType.ControlMask))
            {
                Next();
            }
            if ((e == Gdk.Key.w || e == Gdk.Key.W) && args.Event.State.HasFlag(Gdk.ModifierType.ControlMask))
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
            fc.Filter.AddPattern("*.png");
            fc.Filter.AddPattern("*.bmp");

            fc.LocalOnly = false;

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
        private Image<T> LoadFile_<T>(string p)
            where T:unmanaged
        {
            BitmapFactory bf = new BitmapFactory();
            using (var fs = System.IO.File.OpenRead(p))
            {
                return bf.Decode(fs).ConvertTo<T>();
            }
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
                        DateTime beginPopup = DateTime.Now;
                        Image<byte> grayscale = null;
                        Image<Pixel> applied = null;
                        var _name_ = attentionHandler.Filename;
                        await Task.Run(() => attentionHandler.GenerateGrayscaleAndApplied(out grayscale, out applied));
                       
                        PresentResult pr = new PresentResult();

                        pr.iActivated.Image = applied;
                        pr.iOri.Image = attentionHandler.Image;
                        pr.iActivation.Image = grayscale.ConvertTo<Pixel>();

                        if ((ret = await pr.ShowDialogAsync()))
                        {
                            /*if (chkGenNow.Active)
                            {
                                var newLabel = await tagger.ImproveLabel(iActivated, attentionHandler.Image, grayscale, label);
                                SaveLabel("./data/imp/labels/" + _name_ + ".txt", newLabel, attentionHandler.Image.Width, attentionHandler.Image.Height);
                            }*/
                            SaveFile_("./step1/img/ori/" + _name_ + ".jpg", attentionHandler.Image);
                            SaveFile_("./step1/img/imp/" + _name_ + ".jpg", applied);
                            SaveFile_("./step1/map/" + _name_ + ".jpg", grayscale.ConvertTo<Pixel>());

                            if (chkGenNow.Active)
                            {
                                await GenBox(_name_);
                                await TagNow(_name_);
                            }
                            model.HadChangedTraining();
                        }
                        DateTime endPopup = DateTime.Now;

                        lastUpdate += (endPopup - beginPopup);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.ShowError(gtkWin, e.Message+e.StackTrace);
                }
                model.EndRun();
            }
            return ret;
        }

        private async Task TagNow(string name_)
        {
            TagWindow tagWindow = new TagWindow(name_);
            await tagWindow.ShowDialogAsync();
        }

        private async Task GenBox(string _name_)
        {
            var source = LoadFile_<Pixel>("./step1/img/ori/" + _name_ + ".jpg");
            var grayscale =LoadFile_<byte>("./step1/map/" + _name_ + ".jpg");

            if (Program.verbose)
                Console.WriteLine("Generating box for {0}", _name_);
            var boxes = await tagger.GenerateBoxes(Program.withWindow?iActivated:null, source, grayscale);

            SaveBox(boxes, _name_);
            MoveStep2(_name_);
        }

        private void MoveStep2(string _name_)
        {
            if (Program.verbose)
                Console.WriteLine("Moving {0} fom step1 to step2",_name_);
            System.IO.File.Move("./step1/img/ori/" + _name_ + ".jpg", "./step2/img/ori/" + _name_ + ".jpg");
            System.IO.File.Move("./step1/img/imp/" + _name_ + ".jpg", "./step2/img/imp/" + _name_ + ".jpg");
            System.IO.File.Move("./step1/map/" + _name_ + ".jpg", "./step2/map/" + _name_ + ".jpg");
        }

        private void SaveBox(List<RectangleF> boxes, string _name_)
        {
            System.IO.File.WriteAllText("./step2/box/" + _name_ + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(boxes));
        }

        private void Left_MotionNotifyEvent(object o, MotionNotifyEventArgs args)
        {
            var pos = new PointF(args.Event.X, args.Event.Y);

            DisplayMaskOver(new PointF(pos.X, pos.Y));//Display mask
            if (attentionHandler.IsSet() && model.isNotRunning)
            {
                var scaleX = (attentionHandler.Width / pictureBox.WidthRequest);
                var scaleY = (attentionHandler.Height / pictureBox.HeightRequest);
                var winSizeXImg = scaleX * windowSize;
                var winSizeYImg = scaleY * windowSize;

                var now = DateTime.Now;
                attentionHandler.BuildActivationMap(last, new SizeF(winSizeXImg, winSizeYImg), now - lastUpdate, buttonsPressed[3]);//For previous position
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
                DeleteIfExists("./data/map/images/" + _name_ + ".txt");
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

            LoadImageInformation(attentionHandler.Filename);
        }
        private void NextUntilNotRecorded()
        {
            bool hasLoaded = false;
            while (!hasLoaded)
            {
                while (ExistsInTraining())
                {
                    attentionHandler.FastNext();
                }
                hasLoaded=attentionHandler.LoadCurrent();
            }
            LoadImageInformation(attentionHandler.Filename);
        }

        private bool ExistsInTraining()
        {
            var _name_ = attentionHandler.Filename;
            var img_path = "./data/new_imp/images/" + _name_ + ".jpg";
            var img_path2 = "./step1/img/ori/" + _name_ + ".jpg";
            var img_path3 = "./step2/img/ori/" + _name_ + ".jpg";
            return (System.IO.File.Exists(img_path))|| (System.IO.File.Exists(img_path2))|| (System.IO.File.Exists(img_path3));
        }
        private void LoadImageInformation(string FileName)
        {
            var _name_ = attentionHandler.Filename;
            gtkWin.Title = _name_;
            lastUpdate = DateTime.Now;
            iOri.Image = (attentionHandler.Image);
            pictureBox.Image = (attentionHandler.Image);

            var paths = new string[] {
                 "./data/new_imp/images/" + _name_ + ".jpg",
                 "./step1/img/ori/" + _name_ + ".jpg",
                 "./step2/img/ori/" + _name_ + ".jpg"
            };
            var existingFile = paths.FirstOrDefault((X) => System.IO.File.Exists(X));
            if (existingFile!=null)
            {
                BitmapFactory bitmapFactory = new BitmapFactory();
                using (var fs = System.IO.File.OpenRead(existingFile))
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

            left.Move(rectangle_bottom, 0, Math.Min((int)dispEndY, pictureBox.HeightRequest));
            rectangle_bottom.HeightRequest = Math.Max(0, (int)(pictureBox.HeightRequest - dispEndY));
            rectangle_bottom.WidthRequest = pictureBox.WidthRequest;

            left.Move(rectangle_right, Math.Min((int)dispEndX, pictureBox.WidthRequest), 0);
            rectangle_right.HeightRequest = pictureBox.HeightRequest;
            rectangle_right.WidthRequest = Math.Max(0, (int)(pictureBox.WidthRequest - dispEndX));

            left.Move(viewCircle, (int)dispBeginX, (int)dispBeginY);
            viewCircle.WidthRequest = Math.Max(0, (int)(pictureBox.WidthRequest - dispBeginX));
            viewCircle.HeightRequest = Math.Max(0, (int)(pictureBox.HeightRequest - dispBeginY));
        }
        private async void BtnFixMissing_Clicked(object sender, EventArgs e)
        {
            await Fix();
        }

        public override async Task Fix()
        {
            if (await model.RequestRun())
            {
                CreateDirectories();

                try 
                {
                    var step1 = System.IO.Directory.GetFiles("./step1/img/ori");
                    async Task Dofile(string file)
                    {
                         var img = System.IO.Path.GetFileNameWithoutExtension(file);
                         await GenBox(img);
                    }

                    if (Program.withWindow)//Must run on UI thread
                        foreach (var file in step1)
                            await Dofile(file);
                    else
                        Parallel.ForEach(step1, (r) => Dofile(r).Wait());

                    if (Program.withWindow)
                    {
                        var step2 = System.IO.Directory.GetFiles("./step2/img/ori");
                        foreach (var file in step2)
                        {
                            var img = System.IO.Path.GetFileNameWithoutExtension(file);
                            await TagNow(img);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.Message + e.StackTrace);
                }

                model.HadChangedTraining();
                model.EndRun();
            }
        }

        private static void CreateDirectories()
        {
            Directory.CreateDirectory("./data");

            //Step 1, only image and map
            Directory.CreateDirectory("./step1");
            Directory.CreateDirectory("./step1/img");
            Directory.CreateDirectory("./step1/img/ori");
            Directory.CreateDirectory("./step1/img/imp");
            Directory.CreateDirectory("./step1/map");
            Directory.CreateDirectory("./step1/box");

            //Step 2, after k-means, should be moves from 1 to 2
            Directory.CreateDirectory("./step2");
            Directory.CreateDirectory("./step2/img");
            Directory.CreateDirectory("./step2/img/ori");
            Directory.CreateDirectory("./step2/img/imp");
            Directory.CreateDirectory("./step2/map");
            Directory.CreateDirectory("./step2/box");

            //final output
            Directory.CreateDirectory("./data/new_ori/labels");
            Directory.CreateDirectory("./data/new_ori/images");

            Directory.CreateDirectory("./data/new_imp/labels");
            Directory.CreateDirectory("./data/new_imp/images");
        }

        private void Evt_ButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
        {
            buttonsPressed[args.Event.Button] = false;
        }

        private void Evt_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            buttonsPressed[args.Event.Button] = true;
        }
        private void Da_Drawn(object o, DrawnArgs args)
        {
            if (attentionHandler.Image != null)
            {
                var scaleX = (attentionHandler.Width / pictureBox.WidthRequest);
                var scaleY = (attentionHandler.Height / pictureBox.HeightRequest);
                var winSizeXImg = scaleX * windowSize;
                var winSizeYImg = scaleY * windowSize;
                DrawingArea da = (DrawingArea)o;
                var ctx = args.Cr;
                ctx.SetSourceRGB(0, 0, 0);
                ctx.Rectangle(0, 0, windowSize, windowSize);
                ctx.Fill();
                if (model.isNotRunning)
                {
                    var SubImage = attentionHandler.Image[new Rectangle((int)(dispBeginX * scaleX), (int)(dispBeginY * scaleY), (int)(windowSize * scaleX), (int)(windowSize * scaleY))].ToImage<BGRA>();
                    var img = SubImage.Rescale(windowSize, windowSize, ScalingMode.AverageInterpolate);
                    ImageSurface ims = new ImageSurface(img.DataPointer, Format.Argb32, img.Width, img.Height, img.Width * 4);
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
}
