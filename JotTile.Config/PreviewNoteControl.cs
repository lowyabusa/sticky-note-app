using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using JotTile.Core;

namespace JotTile.Config
{
    internal sealed class PreviewNoteControl : Control
    {
        private AppSettings _settings;

        internal PreviewNoteControl()
        {
            DoubleBuffered = true;
            _settings = AppSettings.CreateDefault();
            Size = new Size(260, 180);
        }

        internal void ApplySettings(AppSettings settings)
        {
            _settings = settings.Clone();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(SystemColors.Control);

            Rectangle noteBounds = new Rectangle(12, 12, Width - 24, Height - 24);
            Color start = ColorUtilities.Parse(_settings.BackgroundColorStart, Color.FromArgb(255, 247, 171));
            Color end = ColorUtilities.Parse(_settings.BackgroundColorEnd, Color.FromArgb(255, 224, 109));

            if (_settings.UseGradient)
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(noteBounds, start, end, ToMode(_settings.GradientDirection)))
                {
                    e.Graphics.FillRectangle(brush, noteBounds);
                }
            }
            else
            {
                using (SolidBrush brush = new SolidBrush(start))
                {
                    e.Graphics.FillRectangle(brush, noteBounds);
                }
            }

            DrawStroke(e.Graphics, noteBounds, ColorUtilities.Parse(_settings.OuterStrokeColor, Color.Brown), _settings.OuterStrokeThickness, 0);
            DrawStroke(e.Graphics, noteBounds, ColorUtilities.Parse(_settings.FrameColor, Color.Goldenrod), _settings.FrameThickness, _settings.OuterStrokeThickness);
            DrawStroke(e.Graphics, noteBounds, ColorUtilities.Parse(_settings.InnerStrokeColor, Color.Beige), _settings.InnerStrokeThickness, _settings.OuterStrokeThickness + _settings.FrameThickness);

            DrawButton(e.Graphics, new Rectangle(noteBounds.Right - 82, noteBounds.Top + 8, 18, 18), ColorUtilities.Parse(_settings.ButtonColor, Color.Gold), ColorUtilities.Parse(_settings.TextColor, Color.SaddleBrown), PreviewGlyph.Save);
            DrawButton(e.Graphics, new Rectangle(noteBounds.Right - 58, noteBounds.Top + 8, 18, 18), ColorUtilities.Parse(_settings.ButtonColor, Color.Gold), ColorUtilities.Parse(_settings.TextColor, Color.SaddleBrown), PreviewGlyph.Copy);
            DrawButton(e.Graphics, new Rectangle(noteBounds.Right - 34, noteBounds.Top + 8, 18, 18), ColorUtilities.Parse(_settings.ButtonColor, Color.Gold), ColorUtilities.Parse(_settings.TextColor, Color.SaddleBrown), PreviewGlyph.Close);

            using (Font font = new Font(_settings.NoteFontFamily, 10.0f, FontStyle.Regular, GraphicsUnit.Point))
            using (SolidBrush textBrush = new SolidBrush(ColorUtilities.Parse(_settings.TextColor, Color.FromArgb(68, 54, 0))))
            {
                Rectangle textBounds = new Rectangle(noteBounds.Left + 16, noteBounds.Top + 42, noteBounds.Width - 32, noteBounds.Height - 56);
                e.Graphics.DrawString("Sample note" + Environment.NewLine + "Preview text", font, textBrush, textBounds);
            }
        }

        private static void DrawStroke(Graphics graphics, Rectangle noteBounds, Color color, int thickness, int inset)
        {
            if (thickness <= 0)
            {
                return;
            }

            Rectangle rect = new Rectangle(
                noteBounds.Left + inset,
                noteBounds.Top + inset,
                noteBounds.Width - (2 * inset) - 1,
                noteBounds.Height - (2 * inset) - 1);

            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            using (Pen pen = new Pen(color, thickness))
            {
                graphics.DrawRectangle(pen, rect);
            }
        }

        private static void DrawButton(Graphics graphics, Rectangle bounds, Color buttonColor, Color glyphColor, PreviewGlyph glyph)
        {
            using (SolidBrush brush = new SolidBrush(buttonColor))
            using (Pen pen = new Pen(glyphColor, 1.3f))
            {
                graphics.FillRectangle(brush, bounds);

                switch (glyph)
                {
                    case PreviewGlyph.Save:
                        graphics.DrawRectangle(pen, bounds.Left + 4, bounds.Top + 3, 9, 10);
                        graphics.DrawLine(pen, bounds.Left + 7, bounds.Top + 3, bounds.Left + 7, bounds.Top + 7);
                        graphics.DrawLine(pen, bounds.Left + 7, bounds.Top + 7, bounds.Left + 11, bounds.Top + 7);
                        break;
                    case PreviewGlyph.Copy:
                        graphics.DrawRectangle(pen, bounds.Left + 6, bounds.Top + 4, 7, 8);
                        graphics.DrawRectangle(pen, bounds.Left + 4, bounds.Top + 6, 7, 8);
                        break;
                    case PreviewGlyph.Close:
                        graphics.DrawLine(pen, bounds.Left + 4, bounds.Top + 4, bounds.Right - 4, bounds.Bottom - 4);
                        graphics.DrawLine(pen, bounds.Right - 4, bounds.Top + 4, bounds.Left + 4, bounds.Bottom - 4);
                        break;
                }
            }
        }

        private static LinearGradientMode ToMode(GradientDirection direction)
        {
            switch (direction)
            {
                case GradientDirection.Horizontal:
                    return LinearGradientMode.Horizontal;
                case GradientDirection.ForwardDiagonal:
                    return LinearGradientMode.ForwardDiagonal;
                case GradientDirection.BackwardDiagonal:
                    return LinearGradientMode.BackwardDiagonal;
                default:
                    return LinearGradientMode.Vertical;
            }
        }

        private enum PreviewGlyph
        {
            Save,
            Copy,
            Close
        }
    }
}
