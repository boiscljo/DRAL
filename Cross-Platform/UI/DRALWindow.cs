using System;
using System.Threading.Tasks;

namespace DRAL.UI
{
    public abstract class DRALWindow
    {
        public abstract void Show();
        public abstract Task Fix();
    }
}