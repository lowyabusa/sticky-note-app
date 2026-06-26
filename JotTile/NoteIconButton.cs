using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

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
            ButtonColor = Color.FromArgb(255, 215, 104);
            HoverColor = Color.FromArgb(255, 228, 140);
            DisabledColor = Color.FromArgb(244, 217, 146);
            GlyphColor = Color.FromArgb(68, 54, 0);
            DisabledGlyphColor = Color.FromArgb(145, 132, 100);
        }

        internal event EventHandler? CommandClick;

        internal bool CommandEnabled { get; set; }

        internal NoteButtonGlyph Glyph { get; set; }

        internal Color ButtonColor { get; set; }

        internal Color HoverColor { get; set; }

        internal Color DisabledColor { get; set; }

        internal Color GlyphColor { get; set; }

        internal Color DisabledGlyphColor { get; set; }

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
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Color backColor = CommandEnabled
                ? (_isHovered ? HoverColor : ButtonColor)
                : DisabledColor;

            using (SolidBrush brush = new SolidBrush(backColor))
            using (GraphicsPath path = CreateRoundedRectangle(ClientRectangle, 4))
            {
                e.Graphics.FillPath(brush, path);
            }

            DrawGlyph(e.Graphics);
        }

        private void DrawGlyph(Graphics graphics)
        {
            Color lineColor = CommandEnabled ? GlyphColor : DisabledGlyphColor;
            using (Pen pen = new Pen(lineColor, 1.6f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                switch (Glyph)
                {
                    case NoteButtonGlyph.Edit:
                        graphics.DrawLine(pen, 7, 17, 16, 8);
                        graphics.DrawLine(pen, 15, 7, 18, 10);
                        graphics.DrawLine(pen, 6, 18, 9, 18);
                        break;
                    case NoteButtonGlyph.Save:
                        graphics.DrawRectangle(pen, 6, 5, 12, 13);
                        graphics.DrawLine(pen, 9, 5, 9, 10);
                        graphics.DrawLine(pen, 9, 10, 15, 10);
                        graphics.DrawRectangle(pen, 9, 13, 6, 4);
                        break;
                    case NoteButtonGlyph.Copy:
                        graphics.DrawRectangle(pen, 9, 6, 8, 10);
                        graphics.DrawRectangle(pen, 6, 9, 8, 10);
                        break;
                    case NoteButtonGlyph.Close:
                        graphics.DrawLine(pen, 7, 7, 17, 17);
                        graphics.DrawLine(pen, 17, 7, 7, 17);
                        break;
                }
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
