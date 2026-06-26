using System;
using System.Drawing;
using System.Windows.Forms;

namespace JotTile.Core
{
    internal sealed class NoteLayoutCalculator
    {
        internal Rectangle CalculateBounds(
            string text,
            Rectangle currentBounds,
            Font font,
            Rectangle workingArea,
            NoteLayoutMetrics metrics)
        {
            string safeText = text ?? string.Empty;
            int maxWidth = Math.Max(metrics.MinimumWindowSize.Width, Math.Min(metrics.MaximumWindowSize.Width, workingArea.Width - (2 * metrics.ScreenMargin)));
            int maxHeight = Math.Max(metrics.MinimumWindowSize.Height, Math.Min(metrics.MaximumWindowSize.Height, workingArea.Height - (2 * metrics.ScreenMargin)));

            int contentMaxWidth = Math.Max(48, maxWidth - metrics.HorizontalChrome);
            int contentMinWidth = Math.Max(48, metrics.MinimumWindowSize.Width - metrics.HorizontalChrome);

            int contentWidth = MeasureLongestExplicitLineWidth(safeText, font);
            contentWidth = Clamp(contentWidth, contentMinWidth, contentMaxWidth);

            int windowWidth = Clamp(contentWidth + metrics.HorizontalChrome, metrics.MinimumWindowSize.Width, maxWidth);
            Size proposedContentSize = new Size(Math.Max(48, windowWidth - metrics.HorizontalChrome), int.MaxValue);
            int contentHeight = MeasureWrappedTextHeight(safeText, font, proposedContentSize.Width);
            int windowHeight = Clamp(contentHeight + metrics.VerticalChrome, metrics.MinimumWindowSize.Height, maxHeight);

            Rectangle targetBounds = new Rectangle(currentBounds.X, currentBounds.Y, windowWidth, windowHeight);
            return ClampToWorkingArea(targetBounds, workingArea, metrics.ScreenMargin);
        }

        internal int MeasureDisplayTextHeight(string text, Font font, int width)
        {
            return MeasureWrappedTextHeight(text ?? string.Empty, font, Math.Max(1, width));
        }

        private static int MeasureLongestExplicitLineWidth(string text, Font font)
        {
            string[] lines = NormalizeLines(text);
            TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl;
            int maxWidth = 0;

            foreach (string line in lines)
            {
                string measured = string.IsNullOrEmpty(line) ? " " : line;
                Size size = TextRenderer.MeasureText(measured, font, new Size(int.MaxValue, int.MaxValue), flags);
                if (size.Width > maxWidth)
                {
                    maxWidth = size.Width;
                }
            }

            return Math.Max(48, maxWidth);
        }

        private static int MeasureWrappedTextHeight(string text, Font font, int width)
        {
            string measuredText = PrepareMeasuredText(text);
            TextFormatFlags flags = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.Left;
            Size size = TextRenderer.MeasureText(measuredText, font, new Size(width, int.MaxValue), flags);
            int labelPreferredHeight = MeasureLabelPreferredHeight(measuredText, font, width);
            return Math.Max(font.Height + 8, Math.Max(size.Height, labelPreferredHeight));
        }

        private static int MeasureLabelPreferredHeight(string text, Font font, int width)
        {
            using (Label label = new Label())
            {
                label.AutoSize = false;
                label.Font = font;
                label.Text = text;
                label.MaximumSize = new Size(width, 0);
                return label.GetPreferredSize(new Size(width, 0)).Height;
            }
        }

        private static Rectangle ClampToWorkingArea(Rectangle bounds, Rectangle workingArea, int margin)
        {
            int minX = workingArea.Left + margin;
            int minY = workingArea.Top + margin;
            int maxX = Math.Max(minX, workingArea.Right - margin - bounds.Width);
            int maxY = Math.Max(minY, workingArea.Bottom - margin - bounds.Height);

            int x = Clamp(bounds.X, minX, maxX);
            int y = Clamp(bounds.Y, minY, maxY);
            return new Rectangle(x, y, bounds.Width, bounds.Height);
        }

        private static string[] NormalizeLines(string text)
        {
            return text
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split(new[] { '\n' }, StringSplitOptions.None);
        }

        private static string PrepareMeasuredText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return " ";
            }

            if (text.EndsWith("\r\n", StringComparison.Ordinal) || text.EndsWith("\n", StringComparison.Ordinal) || text.EndsWith("\r", StringComparison.Ordinal))
            {
                return text + " ";
            }

            return text;
        }

        private static int Clamp(int value, int minValue, int maxValue)
        {
            if (value < minValue)
            {
                return minValue;
            }

            if (value > maxValue)
            {
                return maxValue;
            }

            return value;
        }
    }
}
