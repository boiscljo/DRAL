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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Rectangle = MoyskleyTech.ImageProcessing.Image.Rectangle;

namespace DRAL.UI
{
    public partial class TagWindow : DRALWindow
    {
        PointF clickBegin;
        readonly Dictionary<uint, bool> buttonsPressed = new Dictionary<uint, bool>();
        readonly IMAGE_LABEL_INFO label = new IMAGE_LABEL_INFO();
        LABEL current, last;
        readonly Image<Pixel> img;
        readonly Image<BGRA> img_bgra;
        private readonly ImageSurface ims;
        private readonly List<RectangleF> boxes;
        private TaskCompletionSource<bool> result;
        readonly string image;
        public TagWindow(string image)
        {
            this.image = image;
            Init(image);

            label.name = image;
            label.labels = new List<LABEL>();

            img = Program.LoadFile_<Pixel>("./step2/img/imp/" + image + ".jpg");
            img = img.Resize(imgBox.WidthRequest, imgBox.HeightRequest, ScalingMode.AverageInterpolate);
            img_bgra = img.ConvertBufferTo<BGRA>();
            ims = new ImageSurface(img_bgra.DataPointer, Format.Argb32, img.Width, img.Height, img.Width * 4);
            boxes = Newtonsoft.Json.JsonConvert.DeserializeObject<List<RectangleF>>(System.IO.File.ReadAllText("./step2/box/" + image + ".json"));
            buttonsPressed[1] = false;
            buttonsPressed[2] = false;
            buttonsPressed[3] = false;

            gtkWin.DeleteEvent += GtkWin_DeleteEvent1;
        }

        private void GtkWin_DeleteEvent1(object o, DeleteEventArgs args)
        {
            img.Dispose();
        }

        public override void Show()
        {
            gtkWin.ShowAll();
        }
        public Task<bool> ShowDialogAsync()
        {
            result = new TaskCompletionSource<bool>();
            gtkWin.ShowAll();
            return result.Task;
        }
        private void ButtonSave_Clicked(object sender, EventArgs e)
        {
            result.TrySetResult(true);

            MoveDataset();
            gtkWin.Close();
        }
        private void SaveLabel(string v)
        {
            System.IO.File.WriteAllText(v, label.GenerateFile(img.Width, img.Height));
        }

        bool editingLabel = false;
        private void GtkWin_KeyPressEvent(object o, KeyPressEventArgs args)
        {
            var e = args.Event.Key;

            if ((e == Gdk.Key.x || e == Gdk.Key.X) && args.Event.State.HasFlag(Gdk.ModifierType.ControlMask))
            {
                BtnCancel_Clicked(o, args);
            }
            if ((e == Gdk.Key.n || e == Gdk.Key.s || e == Gdk.Key.N || e == Gdk.Key.S) && args.Event.State.HasFlag(Gdk.ModifierType.ControlMask))
            {
                ButtonSave_Clicked(o, args);
            }
            if (e == Gdk.Key.Escape)
            {
                label.labels.Remove(last);
                last = label.labels.LastOrDefault();
                imgBox.QueueDraw();
            }
            if (e == Gdk.Key.KP_Decimal|| e == Gdk.Key.KP_Add || e == Gdk.Key.plus)
            {
                editingLabel = true;
                args.RetVal = false;
                if (last != null)
                    last.category = string.Empty;
            }
            else if (e == Gdk.Key.KP_Subtract || e == Gdk.Key.minus)
            {
                editingLabel = false;
                args.RetVal = false;
            }
            else if (editingLabel && (e.ToString().StartsWith("Key_") || e.ToString().StartsWith("KP_")))
            {
                if (last != null)
                    last.category += e.ToString().Split("_")[1];
                imgBox.QueueDraw();
                args.RetVal = false;
            }
        }


        private void MoveDataset()
        {
            System.IO.File.Move("./step2/img/ori/" + image + ".jpg", "./data/new_ori/images/" + image + ".jpg",true);
            System.IO.File.Move("./step2/img/imp/" + image + ".jpg", "./data/new_imp/images/" + image + ".jpg", true);
            System.IO.File.Move("./step2/map/" + image + ".jpg", "./data/map/images/" + image + ".jpg", true);
            AttentionMapAnalizer c = new AttentionMapAnalizer();
            label.Resize(img.Width, img.Height,imgBox.WidthRequest,imgBox.HeightRequest);
            c.AdaptLabel(boxes, label);

            SaveLabel("./data/new_ori/labels/" + image + ".txt");
            SaveLabel("./data/new_imp/labels/" + image + ".txt");
        }

        private void ImgBox_MotionNotifyEvent(object o, MotionNotifyEventArgs args)
        {
            var pos = new PointF(args.Event.X, args.Event.Y);
            if (current != null)
                current.box2d.SetEnd(pos);
            imgBox.QueueDraw();
        }

        private void BtnCancel_Clicked(object sender, EventArgs e)
        {
            result.TrySetResult(false);
            gtkWin.Close();
        }

        public override async Task Fix()
        {
            CreateDirectories();
            await Task.Yield();
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
            if (args.Event.Button == 1)
            {
                current.box2d.Fix(imgBox.WidthRequest,imgBox.HeightRequest);
                label.labels.Add(current);
                last = current;
                current = null;
                imgBox.QueueDraw();
            }
        }

        private void Evt_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            buttonsPressed[args.Event.Button] = true;
            if (args.Event.Button == 1)
            {
                clickBegin = new PointF(args.Event.X, args.Event.Y);
                current = new LABEL
                {
                    category = "???"
                };
                current.box2d.SetLocation(clickBegin);
                imgBox.QueueDraw();
            }
        }

        private void ImgBox_Drawn(object o, DrawnArgs args)
        {
            Color activeRect = new Color(255, 0, 0);
            Color setted = new Color(246, 255, 2);

            var ctx = args.Cr;
            ctx.SetSource(ims);
            ctx.Rectangle(0, 0, imgBox.WidthRequest, imgBox.HeightRequest);
            ctx.Fill();


            ctx.SetSourceColor(activeRect);
            if (current != null)
            {
                ctx.Rectangle(current.box2d.x1, current.box2d.y1, current.box2d.width, current.box2d.height);
                ctx.Stroke();
            }

            ctx.SetSourceColor(setted);
            foreach (var lbl in label.labels)
            {
                ctx.Rectangle(lbl.box2d.x1, lbl.box2d.y1, lbl.box2d.width, lbl.box2d.height);
                ctx.MoveTo(lbl.box2d.x1, lbl.box2d.y1);
                ctx.SetFontSize(24);
                ctx.TextPath(lbl.category);
                ctx.Stroke();
            }
        }
    }
}
