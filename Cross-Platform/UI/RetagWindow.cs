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
            Directory.CreateDirectory("./data");
            Directory.CreateDirectory("./data/ori/images");
            Directory.CreateDirectory("./data/ori/labels");
            Directory.CreateDirectory("./data/imp/images");
            Directory.CreateDirectory("./data/imp/labels");
            Directory.CreateDirectory("./data/map/images");

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
            //Console.WriteLine(e.ToString() + args.Event.State.ToString());
            if ((e == Gdk.Key.r|| e == Gdk.Key.R) && args.Event.State.HasFlag(Gdk.ModifierType.ControlMask))
            {
                attentionHandler.Reset();
            }
            if ((e == Gdk.Key.o|| e == Gdk.Key.O) && args.Event.State.HasFlag(Gdk.ModifierType.ControlMask))
            {
                OpenImage();
            }
            if ((e == Gdk.Key.z|| e == Gdk.Key.Z) && args.Event.State.HasFlag(Gdk.ModifierType.ControlMask))
            {
                Previous(true);
            }
            if ((e == Gdk.Key.x|| e == Gdk.Key.X) && args.Event.State.HasFlag(Gdk.ModifierType.ControlMask))
            {
                Previous(false);
            }
            if ((e == Gdk.Key.n|| e== Gdk.Key.s|| e == Gdk.Key.N || e == Gdk.Key.S) && args.Event.State.HasFlag(Gdk.ModifierType.ControlMask))
            {
                if (await GenerateAndSave())
                    Next();
            }
            if ((e == Gdk.Key.y|| e == Gdk.Key.b|| e == Gdk.Key.Y|| e == Gdk.Key.B) && args.Event.State.HasFlag(Gdk.ModifierType.ControlMask))
            {
                Next();
            }
            if ((e == Gdk.Key.w|| e == Gdk.Key.W) && args.Event.State.HasFlag(Gdk.ModifierType.ControlMask))
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
                        DateTime beginPopup = DateTime.Now;
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
                        Directory.CreateDirectory("./data/map/images");

                        PresentResult pr = new PresentResult();

                        pr.iActivated.Image = applied;
                        pr.iOri.Image = attentionHandler.Image;
                        pr.iActivation.Image = grayscale.ConvertTo<Pixel>();

                        if ((ret = await pr.ShowDialogAsync()))
                        {
                            if (chkGenNow.Active)
                            {
                                var newLabel = await retag.ImproveLabel(iActivated, attentionHandler.Image, grayscale, label);
                                SaveLabel("./data/imp/labels/" + _name_ + ".txt", newLabel, attentionHandler.Image.Width, attentionHandler.Image.Height);
                            }
                            SaveFile_("./data/ori/images/" + _name_ + ".jpg", attentionHandler.Image);
                            SaveLabel("./data/ori/labels/" + _name_ + ".txt", label, attentionHandler.Image.Width, attentionHandler.Image.Height);
                            SaveFile_("./data/imp/images/" + _name_ + ".jpg", applied);
                            SaveFile_("./data/map/images/" + _name_ + ".jpg", grayscale.ConvertTo<Pixel>());
                            model.HadChangedTraining();
                        }
                        DateTime endPopup = DateTime.Now;

                        lastUpdate += (endPopup - beginPopup);
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
            if (attentionHandler.IsSet() && model.isNotRunning)
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
                MessageBox.Show(gtkWin, "Could not tag an image without label, choose another");
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

        public async Task Fix()
        {
            if(await model.RequestRun())
            {  
                Directory.CreateDirectory("./data");
                Directory.CreateDirectory("./data/ori/images");
                Directory.CreateDirectory("./data/ori/labels");
                Directory.CreateDirectory("./data/imp/images");
                Directory.CreateDirectory("./data/imp/labels");
                Directory.CreateDirectory("./data/both/images");
                Directory.CreateDirectory("./data/both/labels");
                Directory.CreateDirectory("./data/map/images");

                var originals_label = (from x in Directory.GetFiles("./data/ori/labels") select new FileInfo(x).Name).ToArray();
                var finals_label = (from x in Directory.GetFiles("./data/imp/labels") select new FileInfo(x).Name).ToArray();
                var bf = new BitmapFactory();
                int missing_both=0;
                int countFix = 0;
                //Fix untagged finals
                if (originals_label.Length != finals_label.Length)
                {
                    //await Task.Run(async() =>
                    {
                        async Task action_fix_label(string label_path)
                        {
                            try
                            {
                                var fi = new FileInfo(label_path);

                                var img = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
                                var label = manager.GetLabel(img);

                                var ori_img = bf.Decode("./data/ori/images/" + img + ".jpg");
                                var grayscale = bf.Decode("./data/map/images/" + img + ".jpg");
                                countFix++;
                                if (Program.verbose)
                                    Console.WriteLine("Fixing missing labels for " + label_path);
                                if (Program.withWindow)
                                {
                                    Application.Invoke((_, _1) =>
                                    {
                                        iOri.Image = pictureBox.Image = ori_img;
                                        iActivation.Image = grayscale;
                                    });
                                }

                                var task = retag.ImproveLabel(Program.withWindow ? iActivated : null, ori_img, grayscale.ConvertTo<byte>(), label);
                                var newLabel = await task;
                                if (Program.verbose)
                                    Console.WriteLine("Saving labels to ./data/imp/labels/" + label_path);
                                SaveLabel("./data/imp/labels/" + label_path, newLabel, ori_img.Width, ori_img.Height);
                            }
                            catch (Exception er)
                            {
                                Console.WriteLine("[ERROR]" + er.Message + er.StackTrace + "[/ERROR]");
                            }
                        };
                        var src = originals_label.Except(finals_label);
                        if (Program.withWindow)//Must run on UI thread
                            foreach (var label_path in src)
                                await action_fix_label(label_path);
                        else
                            Parallel.ForEach(src, (r) => action_fix_label(r).Wait());
                    }//);
                }

                var originals = (from x in Directory.GetFiles("./data/ori/images") select new FileInfo(x).Name).ToArray();
                var finals = (from x in Directory.GetFiles("./data/imp/images") select new FileInfo(x).Name).ToArray();
                var maps = (from x in Directory.GetFiles("./data/map/images") select new FileInfo(x).Name).ToArray();


                //fix missing maps
                Parallel.ForEach(originals, (ori) =>
                {
                    if (!maps.Contains(ori))
                    {
                        //Map is missing

                        var ori_img = bf.Decode("./data/ori/images/" + ori);
                        var fin_img = bf.Decode("./data/imp/images/" + ori);
                        var map = Image<byte>.Create(ori_img.Width, ori_img.Height);
                        countFix++;

                        if (Program.verbose)
                            Console.WriteLine("Fixing missing maps for " + ori);

                        for (var x = 0; x < ori_img.Width; x++)
                        {
                            for (var y = 0; y < ori_img.Height; y++)
                            {
                                var ori_px = ori_img[x, y];
                                var end_px = fin_img[x, y];

                                var min_a_r = 0;
                                if (ori_px.R != 128) min_a_r = (end_px.R * 255 - 128 * 255) / (ori_px.R - 128);
                                var min_a_g = 0;
                                if (ori_px.G != 128) min_a_g = (end_px.G * 255 - 128 * 255) / (ori_px.G - 128);
                                var min_a_b = 0;
                                if (ori_px.B != 128) min_a_b = (end_px.B * 255 - 128 * 255) / (ori_px.B - 128);

                                var nb_val_r = 255; if (ori_px.R != 128) nb_val_r = 255 / (Math.Abs(128 - ori_px.R));
                                var nb_val_g = 255; if (ori_px.G != 128) nb_val_g = 255 / (Math.Abs(128 - ori_px.G));
                                var nb_val_b = 255; if (ori_px.B != 128) nb_val_b = 255 / (Math.Abs(128 - ori_px.B));

                                var vals_r = Enumerable.Range(min_a_r, nb_val_r);
                                var vals_g = Enumerable.Range(min_a_g, nb_val_g);
                                var vals_b = Enumerable.Range(min_a_b, nb_val_b);

                                var possible_values = vals_r.Union(vals_g).Union(vals_b).ToArray();

                                if (possible_values.Length == 0)
                                    MessageBox.ShowError(gtkWin, "Impossible solution");
                                map[x, y] = (byte)possible_values.FirstOrDefault();
                            }
                        }

                        map.SaveJPG("./data/map/images/" + ori);

                    }
                });

                void DeleteIfExists(string path)
                {
                    if (Program.verbose)
                        Console.WriteLine("Deleting orphan file " + path);
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }
                void CopyIfNot(string path,string dest)
                {
                    if (!System.IO.File.Exists(dest))
                    {
                        if (Program.verbose)
                            Console.WriteLine("Fixing missing file in both " + dest);
                        missing_both++;
                        System.IO.File.Copy(path, dest,true);
                    }
                }
                //orphans
                int count_orphans = 0;
                originals_label = (from fi in (from x in Directory.GetFiles("./data/ori/labels") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();
                finals_label = (from fi in (from x in Directory.GetFiles("./data/imp/labels") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();
                originals = (from fi in (from x in Directory.GetFiles("./data/ori/images") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();
                finals = (from fi in (from x in Directory.GetFiles("./data/imp/images") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();
                maps = (from fi in (from x in Directory.GetFiles("./data/map/images") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();
                //orphans in original
                if (false && originals_label.Length != originals.Length)
                {
                    //orphans
                    foreach (var label_without_image in originals_label.Except(originals))
                    {
                        count_orphans++;
                        DeleteIfExists("./data/ori/labels/" + label_without_image + ".txt");
                    }
                    foreach (var image_without_label in originals.Except(originals_label))
                    {
                        count_orphans++;
                        DeleteIfExists("./data/ori/images/" + image_without_label + ".jpg");
                    }
                }
                //orphans in improved
                if (false && finals_label.Length != finals.Length)
                {
                    //orphans
                    foreach (var label_without_image in finals_label.Except(finals))
                    {
                        count_orphans++;
                        DeleteIfExists("./data/imp/labels/" + label_without_image + ".txt");
                    }
                    foreach (var image_without_label in finals.Except(finals_label))
                    {
                        count_orphans++;
                        DeleteIfExists("./data/imp/images/" + image_without_label + ".jpg");
                    }
                }
                //originals do not have the same length as improved
                if (false && originals.Length != finals.Length)
                {
                    foreach (var original_without_final in originals.Except(finals))
                    {
                        count_orphans++;
                        DeleteIfExists("./data/ori/labels/" + original_without_final + ".txt");
                        DeleteIfExists("./data/ori/images/" + original_without_final + ".jpg");
                    }
                    foreach (var final_without_original in finals.Except(originals))
                    {
                        count_orphans++;
                        DeleteIfExists("./data/ori/labels/" + final_without_original + ".txt");
                        DeleteIfExists("./data/ori/images/" + final_without_original + ".jpg");
                    }
                }
                if (false && maps.Length != originals.Length)
                {
                    foreach (var map_without_ori in maps.Except(originals))
                    {
                        count_orphans++;
                        DeleteIfExists("./data/map/images/" + map_without_ori + ".jpg");
                    }
                }

                originals_label = (from fi in (from x in Directory.GetFiles("./data/ori/labels") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();
                finals_label = (from fi in (from x in Directory.GetFiles("./data/imp/labels") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();
                originals = (from fi in (from x in Directory.GetFiles("./data/ori/images") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();
                finals = (from fi in (from x in Directory.GetFiles("./data/imp/images") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();

                foreach (var label in originals_label) CopyIfNot("./data/ori/labels/" + label + ".txt", "./data/both/labels/" + label + "_o.txt");
                foreach (var label in finals_label) CopyIfNot("./data/imp/labels/" + label + ".txt", "./data/both/labels/" + label + "_i.txt");
                foreach (var label in originals) CopyIfNot("./data/ori/images/" + label + ".jpg", "./data/both/images/" + label + "_o.jpg");
                foreach (var label in finals) CopyIfNot("./data/imp/images/" + label + ".jpg", "./data/both/images/" + label + "_i.jpg");

                Application.Invoke((_, _1) =>
                {
                    MessageBox.Show(gtkWin, countFix + " errors has been fixed, " + count_orphans + " orphans file removed, "+missing_both+" files missing in both");
                });
                model.HadChangedTraining();
                model.EndRun();
            }
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
