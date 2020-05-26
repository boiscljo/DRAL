using Gtk;
using System;
using System.Threading.Tasks;

namespace DRAL.UI
{
    internal class MessageBox
    {
        internal static bool Show(Window parent,string v)
        {
            MessageDialog md = new MessageDialog(parent,
                DialogFlags.DestroyWithParent, MessageType.Info,
                ButtonsType.Close, v);
            var ret=  md.Run();
            md.Dispose();
            return ret == (int)Gtk.ResponseType.Accept;
        }
        internal static bool ShowWarning(Window parent, string v)
        {
            MessageDialog md = new MessageDialog(parent,
                DialogFlags.DestroyWithParent, MessageType.Warning,
                ButtonsType.Close, v);
            var ret = md.Run();
            md.Dispose();
            return ret == (int)Gtk.ResponseType.Accept;
        }
        internal static bool ShowError(Window parent, string v)
        {
            MessageDialog md = new MessageDialog(parent,
                DialogFlags.DestroyWithParent, MessageType.Error,
                ButtonsType.Close, v);
            var ret = md.Run();
            md.Dispose();
            return ret == (int)Gtk.ResponseType.Accept;
        }
    }
}