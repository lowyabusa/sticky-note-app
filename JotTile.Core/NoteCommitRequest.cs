using System.Drawing;

namespace JotTile.Core
{
    internal sealed class NoteCommitRequest
    {
        internal NoteCommitRequest(string text, Rectangle bounds)
        {
            Text = text;
            Bounds = bounds;
        }

        internal string Text { get; }

        internal Rectangle Bounds { get; }
    }
}
