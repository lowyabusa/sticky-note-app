using System;
using System.Runtime.InteropServices;

namespace JotTile
{
    internal static class NativeMethods
    {
        internal const int AsfwAny = -1;
        internal const int WmNclButtonDown = 0x00A1;
        internal const int HtCaption = 0x0002;
        internal const int WmNcHitTest = 0x0084;
        internal const int HtClient = 0x0001;
        internal const int HtLeft = 0x000A;
        internal const int HtRight = 0x000B;
        internal const int HtTop = 0x000C;
        internal const int HtTopLeft = 0x000D;
        internal const int HtTopRight = 0x000E;
        internal const int HtBottom = 0x000F;
        internal const int HtBottomLeft = 0x0010;
        internal const int HtBottomRight = 0x0011;
        internal const int SwRestore = 9;
        internal const int WsExToolWindow = 0x00000080;
        internal const int WsExAppWindow = 0x00040000;

        [DllImport("user32.dll")]
        internal static extern bool AllowSetForegroundWindow(int processId);

        [DllImport("user32.dll")]
        internal static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr handle);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr handle, int command);

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
    }
}
