using MoyskleyTech.ImageProcessing.Image;
using MoyskleyTech.ImageProcessing.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AttentionAndRetag.Model
{
    public class DisplayModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public int TrainingFileCount => new DirectoryInfo("./data/ori/labels/").EnumerateFiles().Count();

        private Semaphore semaphore = new Semaphore(1, 1);
        private Semaphore semaphoreScr = new Semaphore(1, 1);
        public void EmitChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void SetRunning(bool v)
        {
            isRunning = v;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(isRunning)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(isNotRunning)));
        }
        public bool isRunning { get; set; } = false;
        public bool isNotRunning => !isRunning;

        public void RequestRun(Action a)
        {
            var val = semaphore.WaitOne(0);
            if (val)
            {
                SetRunning(true);
                a();
                EndRun();
            }
        }

        public async void RequestImageIn<T>(System.Windows.Controls.Image img, Func<Image<T>> generator)
            where T : unmanaged
        {
            var val = semaphoreScr.WaitOne(0);
            if (val)
            {
                var applied = await Task.Run(generator);
                img.Source = applied.ToWPFBitmap();
                applied.Dispose();
                semaphoreScr.Release();
            }
        }
        public async void RequestImageIn<T>(System.Windows.Controls.Image[] img, Func<Image<T>[]> generator)
            where T : unmanaged
        {
            var val = semaphoreScr.WaitOne(0);
            if (val)
            {
                var applied = await Task.Run(generator);
                img.Zip(applied, (i, a) =>
                {
                    i.Source = a.ToWPFBitmap();
                    a.Dispose();
                    return i;
                }).ToList();

                semaphoreScr.Release();
            }
        }

        public async Task<bool> RequestRun()
        {
            return await Task.Run(() =>
            {
                var val = semaphore.WaitOne(0);
                //var val = isNotRunning;
                if (val)
                    SetRunning(true);
                return val;
            });
        }
        public void EndRun()
        {
            SetRunning(false);
            semaphore.Release();
        }

        public void HadChangedTraining()
        {
            EmitChanged(nameof(TrainingFileCount));
        }
    }
}
