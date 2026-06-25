using System;
using System.Threading;
using System.Windows.Forms;

namespace StickyNoteApp
{
    internal static class Program
    {
        internal const string AppMutexName = "Local\\SimpleStickyNotes.App";
        internal const string CreateNoteEventName = "Local\\SimpleStickyNotes.CreateNote";

        [STAThread]
        private static void Main(string[] args)
        {
            bool restoreOnly = HasArgument(args, "--restore-only");
            bool createdNew;
            Mutex mutex = new Mutex(true, AppMutexName, out createdNew);

            if (!createdNew)
            {
                SignalRunningInstance();
                return;
            }

            using (mutex)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new StickyNotesAppContext(restoreOnly));
            }
        }

        private static bool HasArgument(string[] args, string expected)
        {
            int index;
            for (index = 0; index < args.Length; index++)
            {
                if (string.Equals(args[index], expected, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void SignalRunningInstance()
        {
            EventWaitHandle eventHandle = null;

            try
            {
                eventHandle = EventWaitHandle.OpenExisting(CreateNoteEventName);
                eventHandle.Set();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
            }
            finally
            {
                if (eventHandle != null)
                {
                    eventHandle.Dispose();
                }
            }
        }
    }
}
