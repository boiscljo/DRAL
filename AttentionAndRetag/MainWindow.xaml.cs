using Microsoft.Win32;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Rectangle = MoyskleyTech.ImageProcessing.Image.Rectangle;
using MoyskleyTech.ImageProcessing.WPF;
using MoyskleyTech.ImageProcessing;
using AttentionOfUser;
using ImageProcessing.JPEGCodec;
using AttentionAndRetag.Retag;
using ImageProcessing.PNGCodec;
using System.Globalization;
using System.ComponentModel;
using AttentionAndRetag.Attention;
using AttentionAndRetag.Config;
using AttentionAndRetag.Model;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace AttentionAndRetag
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AttentionHandler attentionHandler;
        ConfigurationManager manager;
        Retagger retag;
        AttentionMapAnaliser analyser;
        PointF last;
        DisplayModel model;
        const double windowSize = 100;
        DateTime lastUpdate;
        public MainWindow()
        {
            InitializeComponent();
            PngCodec.Register();
            JPEGCodec.Register();

            manager = new ConfigurationManager();
            attentionHandler = new AttentionHandler() { ConfigurationManager = manager };
            retag = new Retagger() { ConfigurationManager = manager };
            analyser = new AttentionMapAnaliser() { ConfigurationManager = manager };
            model = new DisplayModel();
            manager.Init();
            analyser.Init();
            attentionHandler.Init();
            retag.Init();

            last = new PointF(-windowSize, -windowSize);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayMaskOver(last);

            this.DataContext = model;
        }

        private void grid_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(grid);
            DisplayMaskOver(new PointF(pos.X, pos.Y));//Display mask
            if (attentionHandler.IsSet())
            {
                var scaleX = (attentionHandler.Width / pictureBox.ActualWidth);
                var scaleY = (attentionHandler.Height / pictureBox.ActualHeight);
                var winSizeXImg = scaleX * windowSize;
                var winSizeYImg = scaleY * windowSize;

                var now = DateTime.Now;
                attentionHandler.BuildActivationMap(last, new SizeF(winSizeXImg, winSizeYImg), now - lastUpdate, e.RightButton == MouseButtonState.Pressed);//For previous position
                lastUpdate = now;

                var posOnImage = e.GetPosition(pictureBox);
                last = new PointF(posOnImage.X * scaleX, posOnImage.Y * scaleY);

                /* model.RequestImageIn(iActivation, () =>
                 {
                     attentionHandler.GenerateGrayscale(out Image<byte> gray);
                     return gray;
                 });*/
            }
        }


        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            HandleKeyDownEvent(sender, e.Key);
        }

        private async void HandleKeyDownEvent(object sender, Key e)
        {
            if (e == Key.R && Keyboard.Modifiers == ModifierKeys.Control)
            {
                attentionHandler.Reset();
            }
            if (e == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
            {
                OpenImage();
            }
            if (e == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                await GenerateAndSave();
            }
            if (e == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Previous(true);
            }
            if (e == Key.X && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Previous(false);
            }
            if (e == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (await GenerateAndSave())
                    Next();
            }
            if ((e == Key.Y && Keyboard.Modifiers == ModifierKeys.Control) ||
                (e == Key.B && Keyboard.Modifiers == ModifierKeys.Control))
            {
                Next();
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
            }
            LoadImageInformation(attentionHandler.Filename);
        }

        private async void SaveClick(object sender, RoutedEventArgs e)
        {
            await GenerateAndSave();
        }
        bool isRun = false;
        private async Task<bool> GenerateAndSave()
        {
            bool ret = false;
            await Task.Yield();
            if (await model.RequestRun())
            {
                isRun = true;
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

                    pr.iActivated.Source = applied.ToWPFBitmap();
                    pr.iOri.Source = attentionHandler.Image.ToWPFBitmap();
                    pr.iActivation.Source = grayscale.ToWPFBitmap();
                    pr.WindowState = WindowState.Maximized;
                    pr.Closed += Pr_Closed;
                    if ((ret = pr.ShowDialog() == true))
                    {
                        var newLabel = await retag.ImproveLabel(iActivated, attentionHandler.Image, grayscale, label);

                        SaveFile_("./data/ori/images/" + _name_ + ".jpg", attentionHandler.Image);
                        SaveLabel("./data/ori/labels/" + _name_ + ".txt", label, attentionHandler.Image.Width, attentionHandler.Image.Height);
                        SaveFile_("./data/imp/images/" + _name_ + ".jpg", applied);
                        SaveLabel("./data/imp/labels/" + _name_ + ".txt", newLabel, attentionHandler.Image.Width, attentionHandler.Image.Height);
                        model.HadChangedTraining();
                    }

                    btnSave.Focusable = true;
                    SetFocus();
                }
                model.EndRun();
            }
            return ret;
        }
        private void SetFocus()
        {
            this.Focusable = true;
            this.Activate();
            this.Focus();
            btnSave.Focus();
            Interop.SetFocus(this);

            Keyboard.Focus(btnSave);
        }

        private void Pr_Closed(object sender, EventArgs e)
        {
            SetFocus();
        }

        private void LoadLabels(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Load labels";
            ofd.Filter = ".json|*.json";
            if (manager.LabelDirectory.Exists)
                ofd.InitialDirectory = manager.LabelDirectory.FullName;
            if (manager.LastOpenedDirectoryLabel != null)
                ofd.InitialDirectory = manager.LastOpenedDirectoryLabel;
            if (ofd.ShowDialog() == true)
            {
                manager.LoadLabels(ofd.FileName);
                manager.SaveConfig();
            }
        }



        private void OpenImage(object sender, RoutedEventArgs e)
        {
            OpenImage();
        }

        private void OpenImage()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Load image";
            ofd.Filter = ".jpg|*.jpg";
            if (manager.ImgDirectory.Exists)
                ofd.InitialDirectory = manager.ImgDirectory.FullName;
            if (ofd.ShowDialog() == true)
            {
                attentionHandler.OpenImage(ofd.FileName, true);
                manager.SaveConfig();
                LoadImageInformation(ofd.FileName);
            }
        }

        private void LoadImageInformation(string FileName)
        {
            var _name_ = attentionHandler.Filename;
            var lbl = manager.GetLabel(attentionHandler.Filename);
            if (lbl == null)
            {
                MessageBox.Show("Could not tag an image without label, choose another");
            }
            else
            {
                this.Title = lbl.name;
                lastUpdate = DateTime.Now;
                pictureBox.Source = attentionHandler.Image.ToWPFBitmap();
                iOri.Source = pictureBox.Source;
            }
            var img_path = "./data/imp/images/" + _name_ + ".jpg";
            var exists = (System.IO.File.Exists(img_path));
            if (exists)
            {
                BitmapFactory bitmapFactory = new BitmapFactory();
                using (var fs = System.IO.File.OpenRead(img_path))
                    iActivation.Source = bitmapFactory.Decode(fs).ToWPFBitmap();
            }
            else
                iActivation.Source = null;
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
        private void DisplayMaskOver(PointF location)
        {
            var dispBeginX = location.X - windowSize / 2;
            var dispEndX = location.X + windowSize / 2;

            var dispBeginY = location.Y - windowSize / 2;
            var dispEndY = location.Y + windowSize / 2;

            Canvas.SetTop(rect1, 0);//top
            Canvas.SetLeft(rect1, 0);//top
            rect1.Height = Math.Max(dispBeginY, 0);
            rect1.Width = grid.ActualWidth;

            Canvas.SetTop(rect2, 0);//left
            Canvas.SetLeft(rect2, 0);//left
            rect2.Height = grid.ActualHeight;
            rect2.Width = Math.Max(dispBeginX, 0);//left

            Canvas.SetTop(rect3, dispEndY);//bottom
            Canvas.SetLeft(rect3, 0);
            rect3.Height = grid.ActualHeight;
            rect3.Width = grid.ActualWidth;

            Canvas.SetTop(rect4, 0);//right
            Canvas.SetLeft(rect4, dispEndX);//right
            rect4.Height = grid.ActualHeight;
            rect4.Width = grid.ActualWidth;

            Canvas.SetTop(viewCircle, dispBeginY);//top
            Canvas.SetLeft(viewCircle, dispBeginX);//top
            viewCircle.Width = windowSize;
            viewCircle.Height = windowSize;

        }
    }
    public class Interop
    {
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        public static IntPtr GetWindowHandle(Window window)
        {
            return new WindowInteropHelper(window).Handle;
        }

        public static void SetFocus(Window win)
        {
            // In main window, when the MessageBox is closed
            IntPtr window = Interop.GetWindowHandle(win);
            IntPtr focused = Interop.GetForegroundWindow();
            if (window != focused)
            {
                Interop.SetForegroundWindow(window);
            }
        }
    }


}
