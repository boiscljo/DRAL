using Gtk;
using System;
using System.Collections.Generic;
using System.Text;

namespace DRAL.UI
{
    partial class PresentResult
    {
        private Window gtkWin;
        public FixedSizeImage iOri { get; private set; }
        public FixedSizeImage iActivation { get; private set; }
        public FixedSizeImage iActivated { get; private set; }
        private System.Threading.Tasks.TaskCompletionSource<bool> result;
        public void Init()
        {
            gtkWin = new Window("DRAL Retag");

            Box vbox = new Box(Orientation.Vertical, 0);
            Box images = new Box(Orientation.Horizontal, 2);
            Box buttons = new Box(Orientation.Horizontal, 2);
            buttons.BaselinePosition = BaselinePosition.Center;

            vbox.Add(images);
            vbox.Add(buttons);
            var siz = new MoyskleyTech.ImageProcessing.Image.Size(266, 200);
            iOri = new FixedSizeImage(siz);
            iActivation = new FixedSizeImage(siz);
            iActivated = new FixedSizeImage(siz);
            images.Add(iOri);
            images.Add(iActivation);
            images.Add(iActivated);

            gtkWin.DeleteEvent += delegate { result.TrySetResult(false); };

            Button btnOK = new Button() { Label = "OK" };
            btnOK.Clicked += delegate
            {
                result.TrySetResult(true); gtkWin.Dispose();
            };
            Button btnCancel = new Button() { Label = "Cancel" };
            btnCancel.Clicked += delegate
            {
                result.TrySetResult(false); gtkWin.Dispose();
            };

            buttons.Add(btnOK);
            buttons.Add(btnCancel);

            gtkWin.Add(vbox);
        }
    }
}
