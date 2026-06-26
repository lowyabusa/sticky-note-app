using System;
using System.Threading;
using System.Windows.Forms;
using JotTile.Core;

namespace JotTile.Config
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool createdNew;
            Mutex mutex = new Mutex(true, AppIdentity.ConfigMutexName, out createdNew);
            if (!createdNew)
            {
                AppSignals.TryRaise(AppIdentity.ConfigActivateEventName);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (mutex)
            {
                Application.Run(new ConfigForm());
            }
        }
    }
}
