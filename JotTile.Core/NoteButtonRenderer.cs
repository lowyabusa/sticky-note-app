using System;
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
                DrawSystemStyledButton(graphics, bounds, glyph, enabled, hovered, pressed, isDangerButton, glyphColor, disabledGlyphColor);
                return;
            }

            DrawCustomStyledButton(graphics, bounds, glyph, enabled, hovered, pressed, buttonColor, hoverColor, disabledColor, glyphColor, disabledGlyphColor);
        }

        private static void DrawSystemStyledButton(
            Graphics graphics,
            Rectangle bounds,
            NoteButtonGlyph glyph,
            bool enabled,
            bool hovered,
            bool pressed,
            bool isDangerButton,
            Color glyphColor,
            Color disabledGlyphColor)
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
            float scale = Math.Min(bounds.Width, bounds.Height);
            float inset = Math.Max(2f, scale * 0.18f);
            RectangleF inner = RectangleF.Inflate(bounds, -inset, -inset);
            float strokeWidth = Math.Max(1.25f, scale * 0.1f);

            using (Pen pen = new Pen(lineColor, strokeWidth))
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
                        DrawSaveGlyph(graphics, pen, inner);
                        break;
                    case NoteButtonGlyph.Copy:
                        DrawCopyGlyph(graphics, pen, inner);
                        break;
                    case NoteButtonGlyph.Close:
                        graphics.DrawLine(pen, inner.Left + 1, inner.Top + 1, inner.Right - 1, inner.Bottom - 1);
                        graphics.DrawLine(pen, inner.Right - 1, inner.Top + 1, inner.Left + 1, inner.Bottom - 1);
                        break;
                }
            }
        }

        private static void DrawEditGlyph(Graphics graphics, Pen pen, SolidBrush brush, RectangleF inner)
        {
            PointF[] body =
            {
                new PointF(inner.Left + (inner.Width * 0.12f), inner.Bottom - (inner.Height * 0.12f)),
                new PointF(inner.Left + (inner.Width * 0.28f), inner.Bottom + (inner.Height * 0.04f)),
                new PointF(inner.Right - (inner.Width * 0.10f), inner.Top + (inner.Height * 0.26f)),
                new PointF(inner.Right - (inner.Width * 0.26f), inner.Top + (inner.Height * 0.10f))
            };

            PointF[] eraser =
            {
                new PointF(inner.Left + (inner.Width * 0.04f), inner.Bottom - (inner.Height * 0.28f)),
                new PointF(inner.Left + (inner.Width * 0.20f), inner.Bottom - (inner.Height * 0.12f)),
                new PointF(inner.Right - (inner.Width * 0.26f), inner.Top + (inner.Height * 0.10f)),
                new PointF(inner.Right - (inner.Width * 0.42f), inner.Top - (inner.Height * 0.06f))
            };

            PointF[] tip =
            {
                new PointF(inner.Right - (inner.Width * 0.10f), inner.Top + (inner.Height * 0.26f)),
                new PointF(inner.Right + (inner.Width * 0.04f), inner.Top + (inner.Height * 0.12f)),
                new PointF(inner.Right - (inner.Width * 0.02f), inner.Top + (inner.Height * 0.34f))
            };

            graphics.FillPolygon(brush, eraser);
            graphics.FillPolygon(brush, body);
            graphics.FillPolygon(brush, tip);

            graphics.DrawPolygon(pen, eraser);
            graphics.DrawPolygon(pen, body);
            graphics.DrawPolygon(pen, tip);
            graphics.DrawLine(
                pen,
                inner.Left + (inner.Width * 0.12f),
                inner.Bottom - (inner.Height * 0.12f),
                inner.Left - (inner.Width * 0.02f),
                inner.Bottom + (inner.Height * 0.04f));
        }

        private static void DrawSaveGlyph(Graphics graphics, Pen pen, RectangleF inner)
        {
            float bodyWidth = inner.Width * 0.86f;
            float bodyHeight = inner.Height * 0.9f;
            RectangleF body = new RectangleF(
                inner.Left + ((inner.Width - bodyWidth) / 2f),
                inner.Top + ((inner.Height - bodyHeight) / 2f),
                bodyWidth,
                bodyHeight);

            graphics.DrawRectangle(pen, body.X, body.Y, body.Width, body.Height);

            float notchX = body.Left + (body.Width * 0.3f);
            float notchBottom = body.Top + (body.Height * 0.48f);
            graphics.DrawLine(pen, notchX, body.Top, notchX, notchBottom);
            graphics.DrawLine(pen, notchX, notchBottom, body.Right - (body.Width * 0.16f), notchBottom);

            RectangleF label = new RectangleF(
                body.Left + (body.Width * 0.26f),
                body.Bottom - (body.Height * 0.3f),
                body.Width * 0.42f,
                body.Height * 0.18f);

            graphics.DrawRectangle(pen, label.X, label.Y, label.Width, label.Height);
        }

        private static void DrawCopyGlyph(Graphics graphics, Pen pen, RectangleF inner)
        {
            float rectWidth = inner.Width * 0.62f;
            float rectHeight = inner.Height * 0.72f;
            float offsetX = inner.Width * 0.14f;
            float offsetY = inner.Height * 0.14f;
            RectangleF back = new RectangleF(
                inner.Left + offsetX,
                inner.Top + offsetY,
                rectWidth,
                rectHeight);
            RectangleF front = new RectangleF(
                back.Left + (inner.Width * 0.18f),
                back.Top - (inner.Height * 0.08f),
                rectWidth,
                rectHeight);

            graphics.DrawRectangle(pen, back.X, back.Y, back.Width, back.Height);
            graphics.DrawRectangle(pen, front.X, front.Y, front.Width, front.Height);
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
