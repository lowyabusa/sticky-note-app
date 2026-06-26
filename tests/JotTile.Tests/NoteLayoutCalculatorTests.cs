using System.Drawing;
using JotTile.Core;
using Xunit;

namespace JotTile.Tests
{
    public sealed class NoteLayoutCalculatorTests
    {
        private readonly NoteLayoutCalculator _calculator;
        private readonly NoteLayoutMetrics _metrics;
        private readonly Font _font;

        public NoteLayoutCalculatorTests()
        {
            _calculator = new NoteLayoutCalculator();
            _metrics = new NoteLayoutMetrics();
            _font = new Font("Segoe UI", 10.0f);
        }

        [Fact]
        public void EmptyTextUsesMinimumSize()
        {
            Rectangle bounds = _calculator.CalculateBounds(string.Empty, new Rectangle(50, 50, 160, 90), _font, new Rectangle(0, 0, 1920, 1080), _metrics);

            Assert.Equal(160, bounds.Width);
            Assert.Equal(90, bounds.Height);
        }

        [Fact]
        public void MultiLineTextIncreasesHeight()
        {
            Rectangle bounds = _calculator.CalculateBounds("one\r\ntwo\r\nthree", new Rectangle(50, 50, 160, 90), _font, new Rectangle(0, 0, 1920, 1080), _metrics);

            Assert.True(bounds.Height > 90);
        }

        [Fact]
        public void LongTextClampsToWorkingArea()
        {
            string longText = new string('W', 500);
            Rectangle workingArea = new Rectangle(0, 0, 320, 220);

            Rectangle bounds = _calculator.CalculateBounds(longText, new Rectangle(280, 180, 160, 90), _font, workingArea, _metrics);

            Assert.True(bounds.Width <= workingArea.Width - 32);
            Assert.True(bounds.Height <= workingArea.Height - 32);
            Assert.True(bounds.Right <= workingArea.Right - 16);
            Assert.True(bounds.Bottom <= workingArea.Bottom - 16);
        }

        [Fact]
        public void TrailingNewlineStillConsumesHeight()
        {
            Rectangle singleLine = _calculator.CalculateBounds("hello", new Rectangle(20, 20, 160, 90), _font, new Rectangle(0, 0, 800, 600), _metrics);
            Rectangle trailingNewline = _calculator.CalculateBounds("hello\r\n", new Rectangle(20, 20, 160, 90), _font, new Rectangle(0, 0, 800, 600), _metrics);

            Assert.True(trailingNewline.Height >= singleLine.Height);
        }
    }
}
