using System.Drawing;
using System.Windows.Forms;

namespace JotTile
{
    internal sealed partial class StickyNoteForm
    {
        private void HandleDragMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (sender == _inputBox)
            {
                return;
            }

            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(Handle, NativeMethods.WmNclButtonDown, (System.IntPtr)NativeMethods.HtCaption, System.IntPtr.Zero);
        }

        private int GetResizeHandle(Point cursor)
        {
            const int border = 8;
            bool left = cursor.X <= border;
            bool right = cursor.X >= ClientSize.Width - border;
            bool top = cursor.Y <= border;
            bool bottom = cursor.Y >= ClientSize.Height - border;

            if (left && top)
            {
                return NativeMethods.HtTopLeft;
            }

            if (right && top)
            {
                return NativeMethods.HtTopRight;
            }

            if (left && bottom)
            {
                return NativeMethods.HtBottomLeft;
            }

            if (right && bottom)
            {
                return NativeMethods.HtBottomRight;
            }

            if (left)
            {
                return NativeMethods.HtLeft;
            }

            if (right)
            {
                return NativeMethods.HtRight;
            }

            if (top)
            {
                return NativeMethods.HtTop;
            }

            if (bottom)
            {
                return NativeMethods.HtBottom;
            }

            return NativeMethods.HtClient;
        }
    }
}
