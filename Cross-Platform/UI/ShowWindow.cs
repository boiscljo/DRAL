using AttentionAndRetag.Attention;
using AttentionAndRetag.Config;
using AttentionAndRetag.Model;
using AttentionAndRetag.Retag;
using Cairo;
using DRAL.UI;
using Gdk;
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
    public partial class ShowWindow : DRALWindow
    {
        readonly Image<Pixel> image;
        private Image<BGRA> img_bgra;
        private ImageSurface ims;
        private Image<Pixel> img_tmp;
        public ShowWindow()
        {
            Init();
            PngCodec.Register();
            JPEGCodec.Register();

            var img = Program.file_in;

            var dataset = img.Split("/")[0];
            var imgName = img.Split("/")[1];

            var full_file_name_image = "data/" + dataset + "/images/" + imgName + ".jpg";
            var full_file_name_label = "data/" + dataset + "/labels/" + imgName + ".txt";

            image = Program.LoadFile_<Pixel>(full_file_name_image);

            var label = Program.LoadLabelEnd(full_file_name_label);
            label.Resize(image.Width, image.Height, 1, 1);

            Graphics<Pixel> g = Graphics<Pixel>.FromImage(image);
            var measure = g.MeasureString("1", BaseFonts.Premia, 3);
            foreach (var lbl in label.labels)
            {
               
                g.DrawRectangle(Pixels.White, lbl.box2d.x1 , lbl.box2d.y1 , lbl.box2d.width , lbl.box2d.height );
                g.DrawString(lbl.category, new FontSize(BaseFonts.Premia, 3), Pixels.White, new PointF(lbl.box2d.x1 , lbl.box2d.y1  - measure.Height));
            }
        }

        public override void Show()
        {
            this.img.Pixbuf = GtkWrapper.ToPixbuf(image);//.ScaleSimple(WidthRequest, HeightRequest, Gdk.InterpType.Bilinear);

            gtkWin.ShowAll();
          
        }

        public override async Task Fix()
        {
            await Task.Yield();
            if (Program.file_out != null)
            {
                Program.SaveFile_<Pixel>(Program.file_out, image);
            }
            /*else  //Resize files
            {
                Directory.CreateDirectory("./data/new_ori/labels");
                Directory.CreateDirectory("./data/new_imp/labels");

                foreach (var file in Directory.GetFiles("./data/new_ori/labels").Concat(Directory.GetFiles("./data/new_imp/labels")))
                {
                    if (new string[] { 
                        "131.txt",
                        "133.txt",
                        "134.txt",
                        "135.txt","136.txt","137.txt","138.txt","139.txt",
                        "140.txt","141.txt","142.txt","143.txt","144.txt","14.txt",
                        "164.txt","171.txt","207.txt","208.txt","209.txt","210.txt",
                        "211.txt","212.txt","213.txt","214.txt","215.txt","216.txt",
                        "217.txt","218.txt","219.txt",
                        "21.txt","220.txt","221.txt","22.txt","240.txt","241.txt","247.txt",
                        "258.txt","50.txt"
                    }.Contains(new FileInfo(file).Name))
                    {
                        var label = Program.LoadLabelEnd(file);
                        //label.Resize(image.Width, image.Height, 1, 1);
                        var xFactor = 1.53;
                        var yFactor = 1.15;
                        xFactor = yFactor = 0.985;
                        //lbl.box2d.Scale(0.985, 0.985);
                        foreach (var box in label.labels)
                        {
                            box.box2d.Scale(xFactor, yFactor);
                        }
                        Program.SaveLabel(file, label, 1, 1);
                    }
                }
            }*/
        }
        private bool IsRunning = false;
        private void Img_Drawn(object o, DrawnArgs args)
        {
            Console.WriteLine("Img_Drawn");
            if (image != null)
            {
                Console.WriteLine("image!=null");
                var allox = img.Allocation;
                if (!IsRunning && (ims == null || allox.Width != img_bgra.Width || allox.Height != img_bgra.Height))
                {
                    Console.WriteLine("ims==null");
                    IsRunning = true;
                    var ims_tmp = img;
                    ims = null;
                    ims_tmp?.Dispose();
                    img_tmp?.Dispose();
                    img_bgra?.Dispose();


                    

                    Task.Run(() =>
                     {
                         Console.WriteLine("Task begin");
                         img_tmp = image.Resize(allox.Width, allox.Height, ScalingMode.AverageInterpolate);
                         img_bgra = img_tmp.ConvertBufferTo<BGRA>();
                         IsRunning = false;
                         Console.WriteLine("Task end");
                     });
                   
                }
                if (ims == null && !IsRunning)
                {
                    Console.WriteLine("Create ims");
                
                    ims = new ImageSurface(img_bgra.DataPointer, Format.Argb32, img_bgra.Width, img_bgra.Height, img_bgra.Width * 4);
                }
                Console.WriteLine("middle draw");
                if (ims != null)
                {
                    Console.WriteLine("draw image");
                    var ctx = args.Cr;
                    ctx.SetSource(ims);
                    ctx.Rectangle(0, 0, allox.Width, allox.Height);
                    ctx.Fill();
                }
            }
        }
    }
}
