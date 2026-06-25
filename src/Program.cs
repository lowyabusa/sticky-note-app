using System;
using System.Diagnostics;
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
                int runningInstanceProcessId = TryFindRunningInstanceProcessId();
                if (runningInstanceProcessId != 0)
                {
                    NativeMethods.AllowSetForegroundWindow(runningInstanceProcessId);
                }
                else
                {
                    NativeMethods.AllowSetForegroundWindow(NativeMethods.AsfwAny);
                }

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

        private static int TryFindRunningInstanceProcessId()
        {
            int currentProcessId = Process.GetCurrentProcess().Id;
            string processName = Process.GetCurrentProcess().ProcessName;
            Process[] processes = Process.GetProcessesByName(processName);

            try
            {
                int index;
                for (index = 0; index < processes.Length; index++)
                {
                    Process process = processes[index];
                    if (process.Id != currentProcessId)
                    {
                        return process.Id;
                    }
                }
            }
            finally
            {
                int index;
                for (index = 0; index < processes.Length; index++)
                {
                    processes[index].Dispose();
                }
            }

            return 0;
        }
    }
}
