using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using JotTile.Core;

namespace JotTile
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            bool restoreOnly = HasArgument(args, "--restore-only");
            bool createdNew;
            Mutex mutex = new Mutex(true, AppIdentity.AppMutexName, out createdNew);

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
            for (int index = 0; index < args.Length; index++)
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
            EventWaitHandle? eventHandle = null;

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

                eventHandle = EventWaitHandle.OpenExisting(AppIdentity.CreateNoteEventName);
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
                for (int index = 0; index < processes.Length; index++)
                {
                    if (processes[index].Id != currentProcessId)
                    {
                        return processes[index].Id;
                    }
                }
            }
            finally
            {
                for (int index = 0; index < processes.Length; index++)
                {
                    processes[index].Dispose();
                }
            }

            return 0;
        }
    }
}
