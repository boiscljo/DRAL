﻿using AttentionAndRetag.Attention;
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
using static DRAL.Constants;
namespace DRAL.UI
{
    public partial class RetagWindow: DRALWindow
    {
        readonly AttentionHandler attentionHandler;
        readonly ConfigurationManager manager;
        readonly Retagger retag;
        readonly AttentionMapAnalizer analyser;
        PointF last;
        readonly DisplayModel model;
        const int windowSize = 100;
        DateTime lastUpdate;
        private double dispBeginX;
        private double dispEndX;
        private double dispBeginY;
        private double dispEndY;

        readonly Dictionary<uint, bool> buttonsPressed = new Dictionary<uint, bool>();

        public RetagWindow()
        {
            Init();
            PngCodec.Register();
            JPEGCodec.Register();
            CreateDirectories();

            manager = new ConfigurationManager();
            attentionHandler = new AttentionHandler() { ConfigurationManager = manager };
            retag = new Retagger() { ConfigurationManager = manager };
            analyser = new AttentionMapAnalizer() { ConfigurationManager = manager };

            manager.Init();
            analyser.Init();
            retag.Init();
            model = new DisplayModel(this);

            last = new PointF(-windowSize, -windowSize);
        }

        private static void CreateDirectories()
        {
            Directory.CreateDirectory("./data");
            Directory.CreateDirectory("./data/"+BDDR+"/images");
            Directory.CreateDirectory("./data/"+BDDR+"/labels");
            Directory.CreateDirectory("./data/"+BDDP+"/images");
            Directory.CreateDirectory("./data/"+BDDP+"/labels");
            Directory.CreateDirectory("./data/map/images");
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

        private void ButtonLoadLabels_Clicked(object sender, EventArgs e)
        {
            Gtk.FileChooserDialog fc =
                new Gtk.FileChooserDialog("Load labels",
            gtkWin,
            Gtk.FileChooserAction.Open,
            "Cancel", Gtk.ResponseType.Cancel,
            "Open", Gtk.ResponseType.Accept)
                {
                    Filter = new FileFilter()
                };
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
            "Open", Gtk.ResponseType.Accept)
                {
                    Filter = new FileFilter()
                };
            fc.Filter.AddPattern("*.jpg");

            if (fc.Run() == (int)Gtk.ResponseType.Accept)
            {
                attentionHandler.OpenImage(fc.Filename, true);
                manager.SaveConfig();
                LoadImageInformation();
            }

            //Destroy() to close the File Dialog
            fc.Dispose();
        }
             
        private async Task<bool> GenerateAndSave()
        {
            bool ret = false;
            await Task.Yield();
            if (await model.RequestRun())
            {
                try
                {
                    
                    if (attentionHandler.IsSet)
                    {
                        DateTime beginPopup = DateTime.Now;
                        var _name_ = attentionHandler.Filename;
                        (Image<byte> grayscale, Image<Pixel> applied) = await Task.Run(() => attentionHandler.GenerateGrayscaleAndApplied());
                        var label = manager.GetLabel(_name_);
                     

                        PresentResult pr = new PresentResult();

                        pr.iActivated.Image = applied;
                        pr.iOri.Image = attentionHandler.Image;
                        pr.iActivation.Image = grayscale.ConvertTo<Pixel>();

                        if ((ret = await pr.ShowDialogAsync()))
                        {
                            if (chkGenNow.Active)
                            {
                                var newLabel = await retag.ImproveLabel(iActivated, attentionHandler.Image, grayscale, label);
                                Program.SaveLabel("./data/"+BDDP+"/labels/" + _name_ + ".txt", newLabel, attentionHandler.Image.Width, attentionHandler.Image.Height);
                            }
                            Program.SaveFile_("./data/"+BDDR+"/images/" + _name_ + ".jpg", attentionHandler.Image);
                            Program.SaveLabel("./data/"+BDDR+"/labels/" + _name_ + ".txt", label, attentionHandler.Image.Width, attentionHandler.Image.Height);
                            Program.SaveFile_("./data/"+BDDP+"/images/" + _name_ + ".jpg", applied);
                            Program.SaveFile_("./data/"+BDDP+"/images/" + _name_ + ".jpg", grayscale.ConvertTo<Pixel>());
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
            if (attentionHandler.IsSet&& model.IsNotRunning)
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
            LoadImageInformation();
            if (v)
            {
                var _name_ = attentionHandler.Filename;
                DeleteIfExists("./data/"+BDDR+"/images/" + _name_ + ".jpg");
                DeleteIfExists("./data/"+BDDR+"/labels/" + _name_ + ".txt");
                DeleteIfExists("./data/"+BDDP+"/images/" + _name_ + ".jpg");
                DeleteIfExists("./data/"+BDDP+"/labels/" + _name_ + ".txt");
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
            LoadImageInformation();
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
            LoadImageInformation();
        }

        private bool ExistsInTraining()
        {
            var _name_ = attentionHandler.Filename;
            var img_path = "./data/"+BDDP+"/images/" + _name_ + ".jpg";
            return (System.IO.File.Exists(img_path));
        }
        private void LoadImageInformation()
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
            var img_path = "./data/"+BDDP+"/images/" + _name_ + ".jpg";
            var exists = (System.IO.File.Exists(img_path));
            if (exists)
            {
                iActivation.Image = Program.LoadFile_<Pixel>(img_path);
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
            if(await model.RequestRun())
            {  
                Directory.CreateDirectory("./data");
                Directory.CreateDirectory("./data/"+BDDR+"/images");
                Directory.CreateDirectory("./data/"+BDDR+"/labels");
                Directory.CreateDirectory("./data/"+BDDP+"/images");
                Directory.CreateDirectory("./data/"+BDDP+"/labels");
                Directory.CreateDirectory("./data/"+BDD+"/images");
                Directory.CreateDirectory("./data/"+BDD+"/labels");
                Directory.CreateDirectory("./data/map/images");

                var originals_label = (from x in Directory.GetFiles("./data/"+BDDR+"/labels") select new FileInfo(x).Name).ToArray();
                var finals_label = (from x in Directory.GetFiles("./data/"+BDDP+"/labels") select new FileInfo(x).Name).ToArray();
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

                                var ori_img = Program.LoadFile_<Pixel>("./data/"+BDDR+"/images/" + img + ".jpg");
                                var grayscale = Program.LoadFile_<Pixel>("./data/map/images/" + img + ".jpg");
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
                                    Console.WriteLine("Saving labels to ./data/"+BDDP+"/labels/" + label_path);
                                Program.SaveLabel("./data/"+BDDP+"/labels/" + label_path, newLabel, ori_img.Width, ori_img.Height);
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

                var originals = (from x in Directory.GetFiles("./data/"+BDDR+"/images") select new FileInfo(x).Name).ToArray();
                var finals = (from x in Directory.GetFiles("./data/"+BDDP+"/images") select new FileInfo(x).Name).ToArray();
                var maps = (from x in Directory.GetFiles("./data/map/images") select new FileInfo(x).Name).ToArray();


                //fix missing maps
                Parallel.ForEach(originals, (ori) =>
                {
                    if (!maps.Contains(ori))
                    {
                        //Map is missing
                        var ori_img = Program.LoadFile_<Pixel>("./data/"+BDDR+"/images/" + ori);
                        var fin_img = Program.LoadFile_<Pixel>("./data/"+BDDP+"/images/" + ori);
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

                        Program.SaveFile_("./data/map/images/" + ori, map);
                    }
                });

                static void DeleteIfExists(string path)
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
                originals_label = (from fi in (from x in Directory.GetFiles("./data/"+BDDR+"/labels") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();
                finals_label = (from fi in (from x in Directory.GetFiles("./data/"+BDDP+"/labels") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();
                originals = (from fi in (from x in Directory.GetFiles("./data/"+BDDR+"/images") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();
                finals = (from fi in (from x in Directory.GetFiles("./data/"+BDDP+"/images") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();
                maps = (from fi in (from x in Directory.GetFiles("./data/map/images") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();
                //orphans in original
                if (false && originals_label.Length != originals.Length)
                {
                    //orphans
                    foreach (var label_without_image in originals_label.Except(originals))
                    {
                        count_orphans++;
                        DeleteIfExists("./data/"+BDDR+"/labels/" + label_without_image + ".txt");
                    }
                    foreach (var image_without_label in originals.Except(originals_label))
                    {
                        count_orphans++;
                        DeleteIfExists("./data/"+BDDR+"/images/" + image_without_label + ".jpg");
                    }
                }
                //orphans in improved
                if (false && finals_label.Length != finals.Length)
                {
                    //orphans
                    foreach (var label_without_image in finals_label.Except(finals))
                    {
                        count_orphans++;
                        DeleteIfExists("./data/"+BDDP+"/labels/" + label_without_image + ".txt");
                    }
                    foreach (var image_without_label in finals.Except(finals_label))
                    {
                        count_orphans++;
                        DeleteIfExists("./data/"+BDDP+"/images/" + image_without_label + ".jpg");
                    }
                }
                //originals do not have the same length as improved
                if (false && originals.Length != finals.Length)
                {
                    foreach (var original_without_final in originals.Except(finals))
                    {
                        count_orphans++;
                        DeleteIfExists("./data/"+BDDR+"/labels/" + original_without_final + ".txt");
                        DeleteIfExists("./data/"+BDDR+"/images/" + original_without_final + ".jpg");
                    }
                    foreach (var final_without_original in finals.Except(originals))
                    {
                        count_orphans++;
                        DeleteIfExists("./data/"+BDDR+"/labels/" + final_without_original + ".txt");
                        DeleteIfExists("./data/"+BDDR+"/images/" + final_without_original + ".jpg");
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

                originals_label = (from fi in (from x in Directory.GetFiles("./data/"+BDDR+"/labels") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();
                finals_label = (from fi in (from x in Directory.GetFiles("./data/"+BDDP+"/labels") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();
                originals = (from fi in (from x in Directory.GetFiles("./data/"+BDDR+"/images") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();
                finals = (from fi in (from x in Directory.GetFiles("./data/"+BDDP+"/images") select new FileInfo(x)) select fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)).ToArray();

                foreach (var label in originals_label) CopyIfNot("./data/"+BDDR+"/labels/" + label + ".txt", "./data/"+BDD+"/labels/" + label + "_o.txt");
                foreach (var label in finals_label) CopyIfNot("./data/"+BDDP+"/labels/" + label + ".txt", "./data/"+BDD+"/labels/" + label + "_i.txt");
                foreach (var label in originals) CopyIfNot("./data/"+BDDR+"/images/" + label + ".jpg", "./data/"+BDD+"/images/" + label + "_o.jpg");
                foreach (var label in finals) CopyIfNot("./data/"+BDDP+"/images/" + label + ".jpg", "./data/"+BDD+"/images/" + label + "_i.jpg");

                Application.Invoke((_, _1) =>
                {
                    MessageBox.Show(gtkWin, countFix + " errors has been fixed, " + count_orphans + " orphans file removed, "+missing_both+" files missing in both");
                });
                model.HadChangedTraining();
                model.EndRun();
            }
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
                if (model.IsNotRunning)
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
