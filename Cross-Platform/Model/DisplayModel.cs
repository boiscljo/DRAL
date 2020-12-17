using DRAL;
using DRAL.UI;
using MoyskleyTech.ImageProcessing.Image;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static DRAL.Constants;
namespace AttentionAndRetag.Model
{
    /// <summary>
    /// Display model for RETAG mode
    /// </summary>
    public class DisplayModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// State variable for number of File in training
        /// </summary>
        public int TrainingFileCount => new DirectoryInfo("./data/"+BDDR+"/labels/").EnumerateFiles().Count();
        /// <summary>
        /// The window reference
        /// </summary>
        private readonly RetagWindow win;
        /// <summary>
        /// Contructor that emit the event
        /// </summary>
        /// <param name="window"></param>
        public DisplayModel(RetagWindow window)
        {
            win = window;
            EmitChanged(nameof(TrainingFileCount));
        }

        private readonly Semaphore semaphore = new Semaphore(1, 1);
        private readonly Semaphore semaphoreScr = new Semaphore(1, 1);
        /// <summary>
        /// On a platform that accept databinding this event refresh, otherwise it manually edit
        /// SHOULD CHANGE SO THAT THE WINDOW SUBSCRIBE TO THE EVENT
        /// </summary>
        /// <param name="name"></param>
        public void EmitChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            if (name == nameof(TrainingFileCount))
                win.Count = TrainingFileCount.ToString();
        }
        /// <summary>
        /// Current state for the window
        /// </summary>
        /// <param name="v"></param>
        private void SetRunning(bool v)
        {
            IsRunning = v;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNotRunning)));
            if (Program.withWindow)
                foreach (var b in win.buttons)
                    b.Sensitive = !v;
        }
        public bool IsRunning { get; set; } = false;
        public bool IsNotRunning => !IsRunning;

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

        public async void RequestImageIn<T>(FixedSizeImage img,
                                            Func<Image<T>> generator)
            where T : unmanaged
        {
            var val = semaphoreScr.WaitOne(0);
            if (val)
            {
                var applied = await Task.Run(generator);
                img.Image = applied.ConvertTo<Pixel>();

                semaphoreScr.Release();
            }
        }
        public async void RequestImageIn<T>(FixedSizeImage[] img,
                                            Func<Image<T>[]> generator)
            where T : unmanaged
        {
            var val = semaphoreScr.WaitOne(0);
            if (val)
            {
                var applied = await Task.Run(generator);
                img.Zip(applied, (i, a) =>
                {
                    i.Image = a.ConvertTo<Pixel>();

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
            if (Program.verbose)
            {
                Console.WriteLine(TrainingFileCount + " files in training");
            }
        }
    }
}
