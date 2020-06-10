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
        const int windowSize = 100;

        Dictionary<uint, bool> buttonsPressed = new Dictionary<uint, bool>();
        IMAGE_LABEL_INFO label = new IMAGE_LABEL_INFO();
        LABEL current, last;
        Image<Pixel> img;
        Image<BGRA> img_bgra;
        Size originalImageSize;
        private ImageSurface ims;
        private TaskCompletionSource<bool> result;
        string image;
        public TagWindow(string image)
        {
            this.image = image;
            Init(image);

            label.name = image;
            label.labels = new List<LABEL>();

            img = LoadFile_<Pixel>("./step2/img/imp/" + image + ".jpg");
            originalImageSize = img.Size;
            img = img.Resize(imgBox.WidthRequest, imgBox.HeightRequest, ScalingMode.AverageInterpolate);
            img_bgra = img.ConvertBufferTo<BGRA>();
            ims = new ImageSurface(img_bgra.DataPointer, Format.Argb32, img.Width, img.Height, img.Width * 4);

            buttonsPressed[1] = false;
            buttonsPressed[2] = false;
            buttonsPressed[3] = false;
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

            //MoveDataset();
            gtkWin.Close();
        }
        private void SaveLabel(string v)
        {
            System.IO.File.WriteAllText(v, GenerateFile(label, img.Width, img.Height));
        }

        private string GenerateFile(IMAGE_LABEL_INFO label, double w, double h)
        {
            /*  case "traffic sign": return 0;
                case "traffic light": return 0;
                case "car": return 1;
                case "rider": return 2;
                case "motor": return 1;
                case "person": return 3;
                case "bus": return 1;
                case "truck": return 1;
                case "bike": return 2;
                case "train": return 1;*/
            var newLabel = label.Clone();
            newLabel.Resize(w, h, originalImageSize.Width, originalImageSize.Height);
            var possibleBox = newLabel.labels.Where((x) => x.box2d != null).ToList();
            var possibleLines = possibleBox.Select((x) =>
            {
                return x.category + " " + TS(x.box2d.x1 / w) + " " + TS(x.box2d.y1 / h) + " " + TS((x.box2d.x2 - x.box2d.x1) / w) + " " + TS((x.box2d.y2 - x.box2d.y1) / h);
            });
            return string.Join("\r\n", possibleLines);
        }

        private string TS(double v)
        {
            return v.ToString(new CultureInfo("en-US"));
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

        private Image<T> LoadFile_<T>(string p)
            where T : unmanaged
        {
            BitmapFactory bf = new BitmapFactory();
            using (var fs = System.IO.File.OpenRead(p))
            {
                return bf.Decode(fs).ConvertTo<T>();
            }
        }


        private void MoveDataset()
        {
            System.IO.File.Move("./step2/img/ori/" + image + ".jpg", "./data/new_ori/images/" + image + ".jpg");
            System.IO.File.Move("./step2/img/imp/" + image + ".jpg", "./data/new_imp/images/" + image + ".jpg");
            System.IO.File.Move("./step2/map/" + image + ".jpg", "./data/map/images/" + image + ".jpg");
            SaveLabel("./data/new_ori/labels/" + image + ".txt");
            SaveLabel("./data/new_imp/labels/" + image + ".txt");
        }

        private void SaveBox(List<RectangleF> boxes, string _name_)
        {
            System.IO.File.WriteAllText("./step1/box/" + _name_ + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(boxes));
        }

        private void ImgBox_MotionNotifyEvent(object o, MotionNotifyEventArgs args)
        {
            var pos = new PointF(args.Event.X, args.Event.Y);
            if (current != null)
                current.box2d.SetEnd(pos);
            imgBox.QueueDraw();
        }


        private void DeleteIfExists(string v)
        {
            if (System.IO.File.Exists(v))
                System.IO.File.Delete(v);
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
                current.box2d.Fix();
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
                current = new LABEL();
                current.category = "???";
                current.box2d.SetLocation(clickBegin);
                imgBox.QueueDraw();
            }
        }

        private void ImgBox_Drawn(object o, DrawnArgs args)
        {
            Color activeRect = new Color(255, 0, 0);
            Color setted = new Color(0, 255, 0);

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
                ctx.TextPath(lbl.category);
                ctx.Stroke();

            }
        }
    }
}
