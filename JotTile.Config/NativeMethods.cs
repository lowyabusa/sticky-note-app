using System;
using System.Runtime.InteropServices;

namespace JotTile.Config
{
    internal static class NativeMethods
    {
        internal const int SwRestore = 9;

        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr handle);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr handle, int command);
    }
}
