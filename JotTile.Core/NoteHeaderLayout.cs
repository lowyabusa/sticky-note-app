namespace JotTile.Core
{
    internal static class NoteHeaderLayout
    {
        internal static int GetButtonLeft(int containerWidth, int buttonWidth, int rightMargin, int spacing, int indexFromRight)
        {
            int rightEdge = containerWidth - rightMargin;
            return rightEdge - buttonWidth - ((buttonWidth + spacing) * indexFromRight);
        }
    }
}
