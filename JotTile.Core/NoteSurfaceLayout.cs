using System.Drawing;

namespace JotTile.Core
{
    internal sealed class NoteSurfaceLayout
    {
        internal NoteSurfaceLayout(Rectangle noteBounds, Rectangle contentBounds, Rectangle textBounds, Rectangle headerBounds)
        {
            NoteBounds = noteBounds;
            ContentBounds = contentBounds;
            TextBounds = textBounds;
            HeaderBounds = headerBounds;
        }

        internal Rectangle NoteBounds { get; }

        internal Rectangle HeaderBounds { get; }

        internal Rectangle ContentBounds { get; }

        internal Rectangle TextBounds { get; }
    }
}
