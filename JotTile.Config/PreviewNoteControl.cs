using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using JotTile.Core;

namespace JotTile.Config
{
    internal sealed class PreviewNoteControl : Control
    {
        private static readonly Color ActiveHeaderGlyphColor = Color.Black;
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

            NoteSurfaceLayoutMetrics metrics = CreateSurfaceLayoutMetrics();
            Rectangle noteBounds = NoteSurfaceLayoutCalculator.CreateSquarePreviewBounds(Size, metrics.PreviewOuterMargin);
            NoteSurfaceLayout layout = NoteSurfaceLayoutCalculator.Calculate(noteBounds, _settings, metrics);
            Rectangle closeBounds = NoteSurfaceLayoutCalculator.CreateHeaderButtonBounds(noteBounds, _settings, metrics, 0);
            Rectangle copyBounds = NoteSurfaceLayoutCalculator.CreateHeaderButtonBounds(noteBounds, _settings, metrics, 1);
            Rectangle editBounds = NoteSurfaceLayoutCalculator.CreateHeaderButtonBounds(noteBounds, _settings, metrics, 2);
            Color start = ColorUtilities.Parse(_settings.BackgroundColorStart, Color.FromArgb(255, 247, 171));
            Color end = ColorUtilities.Parse(_settings.BackgroundColorEnd, Color.FromArgb(255, 224, 109));
            Color textColor = ColorUtilities.Parse(_settings.TextColor, Color.FromArgb(68, 54, 0));
            Color buttonColor = ColorUtilities.Parse(_settings.ButtonColor, Color.FromArgb(217, 217, 217));
            Color buttonHoverColor = ColorUtilities.Parse(_settings.ButtonHoverColor, Color.FromArgb(236, 236, 236));
            Color buttonDisabledColor = ColorUtilities.Parse(_settings.ButtonDisabledColor, Color.FromArgb(104, 104, 104));
            ButtonRenderMode renderMode = _settings.GetButtonRenderMode();

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

            NoteButtonRenderer.RenderButton(e.Graphics, editBounds, NoteButtonGlyph.Edit, renderMode, true, false, false, false, buttonColor, buttonHoverColor, buttonDisabledColor, ActiveHeaderGlyphColor, ControlPaint.Dark(buttonDisabledColor));
            NoteButtonRenderer.RenderButton(e.Graphics, copyBounds, NoteButtonGlyph.Copy, renderMode, true, false, false, false, buttonColor, buttonHoverColor, buttonDisabledColor, ActiveHeaderGlyphColor, ControlPaint.Dark(buttonDisabledColor));
            NoteButtonRenderer.RenderButton(e.Graphics, closeBounds, NoteButtonGlyph.Close, renderMode, true, false, false, true, buttonColor, buttonHoverColor, buttonDisabledColor, ActiveHeaderGlyphColor, ControlPaint.Dark(buttonDisabledColor));

            using (Font font = new Font(_settings.NoteFontFamily, _settings.NoteFontSize, FontStyle.Regular, GraphicsUnit.Point))
            using (SolidBrush textBrush = new SolidBrush(textColor))
            {
                e.Graphics.DrawString("Sample note" + Environment.NewLine + "Preview text", font, textBrush, layout.TextBounds);
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

        private static NoteSurfaceLayoutMetrics CreateSurfaceLayoutMetrics()
        {
            return new NoteSurfaceLayoutMetrics
            {
                HeaderTop = 8,
                HeaderButtonSize = 18,
                HeaderButtonSpacing = 6,
                HeaderRightMargin = 10,
                HeaderBottomGap = 8,
                ContentSidePadding = 10,
                ContentBottomPadding = 8,
                PreviewOuterMargin = 12
            };
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
    }
}
