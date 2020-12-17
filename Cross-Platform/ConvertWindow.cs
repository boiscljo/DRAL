using AttentionAndRetag.Attention;
using AttentionAndRetag.Config;
using AttentionAndRetag.Retag;
using DRAL.UI;
using Gtk;
using ImageProcessing.JPEGCodec;
using ImageProcessing.PNGCodec;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.IO;
using System.Threading.Tasks;
using static DRAL.Constants;
namespace DRAL
{
    internal class ConvertWindow : DRALWindow
    {
        readonly ConfigurationManager manager;
        readonly Retagger retag;
        private FileInfo[ ] files;
        private string filename;
        Window win=new Window("-");

        public ConvertWindow()
        {
            PngCodec.Register();
            JPEGCodec.Register();
            CreateDirectories();

            manager = new ConfigurationManager();
            retag = new Retagger() { ConfigurationManager = manager };

            manager.Init();
            retag.Init();
        }
        private static void CreateDirectories()
        {
            Directory.CreateDirectory("./data");
            Directory.CreateDirectory("./data/" + BDDR + "/images");
            Directory.CreateDirectory("./data/" + BDDR + "/labels");
            Directory.CreateDirectory("./data/" + BDDP + "/images");
            Directory.CreateDirectory("./data/" + BDDP + "/labels");
            Directory.CreateDirectory("./data/map/images");
        }
        public override async Task Fix()
        {
            await Task.Yield();

            if ( Program.file_in == null )
            {
                Gtk.FileChooserDialog fc =
                new Gtk.FileChooserDialog("Load image",
            win,
            Gtk.FileChooserAction.Open,
            "Cancel", Gtk.ResponseType.Cancel,
            "Open", Gtk.ResponseType.Accept)
                {
                    Filter = new FileFilter()
                };
                fc.Filter.AddPattern("*.jpg");
                fc.Filter.AddPattern("*.png");
                fc.Filter.AddPattern("*.bmp");

                fc.LocalOnly = false;
                if ( fc.Run() == ( int ) Gtk.ResponseType.Accept )
                {
                    var fileInfo = new System.IO.FileInfo(fc.Filename );
                    fc.Dispose();


                    var dir = fileInfo.Directory;
                    files = dir.GetFiles();
                }
            }
            else
            {
                var dir = new System.IO.DirectoryInfo(Program.file_in);
                files = dir.GetFiles();
            }

            foreach ( var file in files )
            {
                if ( Program.verbose )
                    Console.WriteLine("file={0}, files.Length={1}" , file.Name , files.Length);

                ConvertImage(file);
            }


            //Destroy() to close the File Dialog
        }
        private void ConvertImage(FileInfo fi)
        {
            var fileInfo = new System.IO.FileInfo(fi.FullName);
            filename = fileInfo.Name.Substring(0 , fileInfo.Name.Length - fileInfo.Extension.Length);

            var _name_ = filename;
            var lbl = manager.GetLabel(_name_);
            var outfile = "./data/" + BDDR + "/images/" + _name_ + ".jpg" ;

            if ( !System.IO.File.Exists(outfile) )
            {
                var Image = Program.LoadFile_<Pixel>(fi.FullName);
                if ( lbl != null )
                {
                    Program.SaveFile_(outfile , Image);
                    Program.SaveLabel("./data/" + BDDR + "/labels/" + _name_ + ".txt" , lbl , Image.Width , Image.Height);
                }
            }
        }

        public override void Show()
        {
            //win.ShowAll();
        }
    }
}