using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DRAL.UI
{
    public partial class PresentResult
    {
        public PresentResult()
        {
            Init();
        }
        public Task<bool> ShowDialogAsync()
        {
            result = new TaskCompletionSource<bool>();
            gtkWin.ShowAll();
            return result.Task;
        }
    }
}
