using System;
using System.Drawing;
using System.Windows.Forms;
using JotTile.Core;

namespace JotTile
{
    internal sealed class NoteIconButton : Control
    {
        private bool _isHovered;
        private bool _isPressed;

        internal NoteIconButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Size = new Size(24, 24);
            Cursor = Cursors.Hand;
            TabStop = false;
            CommandEnabled = true;
            ButtonColor = Color.FromArgb(217, 217, 217);
            HoverColor = Color.FromArgb(236, 236, 236);
            DisabledColor = Color.FromArgb(104, 104, 104);
            GlyphColor = Color.FromArgb(68, 54, 0);
            DisabledGlyphColor = Color.FromArgb(145, 132, 100);
            RenderMode = ButtonRenderMode.SystemDefault;
        }

        internal event EventHandler? CommandClick;

        internal bool CommandEnabled { get; set; }

        internal NoteButtonGlyph Glyph { get; set; }

        internal Color ButtonColor { get; set; }

        internal Color HoverColor { get; set; }

        internal Color DisabledColor { get; set; }

        internal Color GlyphColor { get; set; }

        internal Color DisabledGlyphColor { get; set; }

        internal ButtonRenderMode RenderMode { get; set; }

        internal bool IsDangerButton { get; set; }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            _isPressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && CommandEnabled)
            {
                _isPressed = true;
                Invalidate();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool shouldClick = _isPressed && CommandEnabled && ClientRectangle.Contains(e.Location);
            _isPressed = false;
            Invalidate();

            if (shouldClick && CommandClick != null)
            {
                CommandClick(this, EventArgs.Empty);
            }

            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            NoteButtonRenderer.RenderButton(
                e.Graphics,
                ClientRectangle,
                Glyph,
                RenderMode,
                CommandEnabled,
                _isHovered,
                _isPressed,
                IsDangerButton,
                ButtonColor,
                HoverColor,
                DisabledColor,
                GlyphColor,
                DisabledGlyphColor);
        }
    }
}
