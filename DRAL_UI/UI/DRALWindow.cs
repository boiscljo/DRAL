using System;
using System.Threading.Tasks;

namespace DRAL.UI
{
    /// <summary>
    /// Base class for all forms
    /// </summary>
    public abstract class DRALWindow
    {
        public abstract void Show();
        public abstract Task Fix();
    }
}