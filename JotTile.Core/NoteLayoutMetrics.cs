using System.Drawing;

namespace JotTile.Core
{
    internal sealed class NoteLayoutMetrics
    {
        internal Size MinimumWindowSize { get; set; } = new Size(160, 90);

        internal Size MaximumWindowSize { get; set; } = new Size(640, 480);

        internal int ScreenMargin { get; set; } = 16;

        internal int HorizontalChrome { get; set; } = 28;

        internal int VerticalChrome { get; set; } = 58;
    }
}
