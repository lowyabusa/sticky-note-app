using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace JotTile.Core
{
    internal static class NoteButtonRenderer
    {
        private static readonly Color SystemButtonColor = Color.FromArgb(217, 217, 217);
        private static readonly Color SystemHoverColor = Color.FromArgb(236, 236, 236);
        private static readonly Color SystemPressedColor = Color.FromArgb(199, 199, 199);
        private static readonly Color SystemDisabledColor = Color.FromArgb(104, 104, 104);
        private static readonly Color SystemCloseColor = Color.FromArgb(224, 67, 67);
        private static readonly Color SystemCloseHoverColor = Color.FromArgb(236, 97, 97);
        private static readonly Color SystemClosePressedColor = Color.FromArgb(199, 46, 46);

        internal static void RenderButton(
            Graphics graphics,
            Rectangle bounds,
            NoteButtonGlyph glyph,
            ButtonRenderMode mode,
            bool enabled,
            bool hovered,
            bool pressed,
            bool isDangerButton,
            Color buttonColor,
            Color hoverColor,
            Color disabledColor,
            Color glyphColor,
            Color disabledGlyphColor)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (mode == ButtonRenderMode.SystemDefault)
            {
                DrawSystemStyledButton(graphics, bounds, glyph, enabled, hovered, pressed, isDangerButton);
                return;
            }

            DrawCustomStyledButton(graphics, bounds, glyph, enabled, hovered, pressed, buttonColor, hoverColor, disabledColor, glyphColor, disabledGlyphColor);
        }

        private static void DrawSystemStyledButton(Graphics graphics, Rectangle bounds, NoteButtonGlyph glyph, bool enabled, bool hovered, bool pressed, bool isDangerButton)
        {
            Color backColor;
            Color borderColor;
            Color lineColor;

            if (!enabled)
            {
                backColor = SystemDisabledColor;
                borderColor = Color.FromArgb(83, 83, 83);
                lineColor = Color.FromArgb(38, 38, 38);
            }
            else if (isDangerButton)
            {
                backColor = pressed ? SystemClosePressedColor : (hovered ? SystemCloseHoverColor : SystemCloseColor);
                borderColor = Color.FromArgb(112, 24, 24);
                lineColor = Color.White;
            }
            else
            {
                backColor = pressed ? SystemPressedColor : (hovered ? SystemHoverColor : SystemButtonColor);
                borderColor = Color.FromArgb(120, 120, 120);
                lineColor = Color.FromArgb(34, 34, 34);
            }

            using (SolidBrush brush = new SolidBrush(backColor))
            using (Pen pen = new Pen(borderColor))
            {
                graphics.FillRectangle(brush, bounds);
                graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            }

            DrawGlyph(graphics, glyph, bounds, lineColor);
        }

        private static void DrawCustomStyledButton(
            Graphics graphics,
            Rectangle bounds,
            NoteButtonGlyph glyph,
            bool enabled,
            bool hovered,
            bool pressed,
            Color buttonColor,
            Color hoverColor,
            Color disabledColor,
            Color glyphColor,
            Color disabledGlyphColor)
        {
            Color backColor = enabled
                ? (pressed ? ControlPaint.Dark(hoverColor) : (hovered ? hoverColor : buttonColor))
                : disabledColor;

            using (SolidBrush brush = new SolidBrush(backColor))
            using (GraphicsPath path = CreateRoundedRectangle(bounds, 4))
            {
                graphics.FillPath(brush, path);
            }

            DrawGlyph(graphics, glyph, bounds, enabled ? glyphColor : disabledGlyphColor);
        }

        private static void DrawGlyph(Graphics graphics, NoteButtonGlyph glyph, Rectangle bounds, Color lineColor)
        {
            Rectangle inner = Rectangle.Inflate(bounds, -4, -4);

            using (Pen pen = new Pen(lineColor, 1.6f))
            using (SolidBrush brush = new SolidBrush(lineColor))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                switch (glyph)
                {
                    case NoteButtonGlyph.Edit:
                        DrawEditGlyph(graphics, pen, brush, inner);
                        break;
                    case NoteButtonGlyph.Save:
                        graphics.DrawRectangle(pen, inner.Left + 1, inner.Top, 10, 11);
                        graphics.DrawLine(pen, inner.Left + 4, inner.Top, inner.Left + 4, inner.Top + 5);
                        graphics.DrawLine(pen, inner.Left + 4, inner.Top + 5, inner.Left + 9, inner.Top + 5);
                        graphics.DrawRectangle(pen, inner.Left + 4, inner.Top + 8, 5, 3);
                        break;
                    case NoteButtonGlyph.Copy:
                        graphics.DrawRectangle(pen, inner.Left + 4, inner.Top + 1, 7, 9);
                        graphics.DrawRectangle(pen, inner.Left + 1, inner.Top + 4, 7, 9);
                        break;
                    case NoteButtonGlyph.Close:
                        graphics.DrawLine(pen, inner.Left + 1, inner.Top + 1, inner.Right - 1, inner.Bottom - 1);
                        graphics.DrawLine(pen, inner.Right - 1, inner.Top + 1, inner.Left + 1, inner.Bottom - 1);
                        break;
                }
            }
        }

        private static void DrawEditGlyph(Graphics graphics, Pen pen, SolidBrush brush, Rectangle inner)
        {
            Point[] body =
            {
                new Point(inner.Left + 2, inner.Bottom - 1),
                new Point(inner.Left + 4, inner.Bottom + 1),
                new Point(inner.Right - 1, inner.Top + 6),
                new Point(inner.Right - 3, inner.Top + 4)
            };

            Point[] eraser =
            {
                new Point(inner.Left + 1, inner.Bottom - 3),
                new Point(inner.Left + 3, inner.Bottom - 1),
                new Point(inner.Right - 3, inner.Top + 4),
                new Point(inner.Right - 5, inner.Top + 2)
            };

            Point[] tip =
            {
                new Point(inner.Right - 1, inner.Top + 6),
                new Point(inner.Right + 1, inner.Top + 4),
                new Point(inner.Right, inner.Top + 7)
            };

            graphics.FillPolygon(brush, eraser);
            graphics.FillPolygon(brush, body);
            graphics.FillPolygon(brush, tip);

            graphics.DrawPolygon(pen, eraser);
            graphics.DrawPolygon(pen, body);
            graphics.DrawPolygon(pen, tip);
            graphics.DrawLine(pen, inner.Left + 2, inner.Bottom - 1, inner.Left, inner.Bottom + 1);
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
