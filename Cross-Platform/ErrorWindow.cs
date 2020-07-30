using DRAL.UI;
using System.Threading.Tasks;

namespace DRAL
{
    internal class ErrorWindow : DRALWindow
    {
        public override async Task Fix()
        {
            await Task.Yield();
        }

        public override void Show()
        {
            
        }
    }
}