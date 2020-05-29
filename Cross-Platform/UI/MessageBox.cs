using Gtk;
using System;
using System.Threading.Tasks;

namespace DRAL.UI
{
    internal class MessageBox
    {
        internal static bool Show(Window parent, string v)
        {
            if (Program.withWindow)
            {
                MessageDialog md = new MessageDialog(parent,
                    DialogFlags.DestroyWithParent, MessageType.Info,
                    ButtonsType.Close, v);
                var ret = md.Run();
                md.Dispose();
                return ret == (int)Gtk.ResponseType.Accept;
            }
            else
            {
                Console.WriteLine(v);
                return false;
            }
        }
        internal static bool ShowWarning(Window parent, string v)
        {
            if (Program.withWindow)
            {
                MessageDialog md = new MessageDialog(parent,
                DialogFlags.DestroyWithParent, MessageType.Warning,
                ButtonsType.Close, v);
                var ret = md.Run();
                md.Dispose();
                return ret == (int)Gtk.ResponseType.Accept;
            }
            else
            {
                Console.WriteLine(v);
                return false;
            }
        }
        internal static bool ShowError(Window parent, string v)
        {
            if (Program.withWindow)
            {
                MessageDialog md = new MessageDialog(parent,
                DialogFlags.DestroyWithParent, MessageType.Error,
                ButtonsType.Close, v);
                var ret = md.Run();
                md.Dispose();
                return ret == (int)Gtk.ResponseType.Accept;
            }
            else
            {
                Console.Error.WriteLine(v);
                return false;
            }
        }
    }
}