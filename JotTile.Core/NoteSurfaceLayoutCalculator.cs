using System;
using System.Drawing;

namespace JotTile.Core
{
    internal static class NoteSurfaceLayoutCalculator
    {
        internal static NoteSurfaceLayout Calculate(Rectangle noteBounds, AppSettings settings, NoteSurfaceLayoutMetrics metrics)
        {
            int decorativeInset = metrics.GetDecorativeInset(settings);

            Rectangle headerBounds = new Rectangle(
                noteBounds.Left + decorativeInset,
                noteBounds.Top + decorativeInset,
                Math.Max(1, noteBounds.Width - (2 * decorativeInset)),
                metrics.HeaderTop + metrics.HeaderButtonSize);

            int contentLeft = noteBounds.Left + decorativeInset + metrics.ContentSidePadding;
            int contentTop = noteBounds.Top + decorativeInset + metrics.HeaderTop + metrics.HeaderButtonSize + metrics.HeaderBottomGap;
            int contentRight = noteBounds.Right - decorativeInset - metrics.ContentSidePadding;
            int contentBottom = noteBounds.Bottom - decorativeInset - metrics.ContentBottomPadding;

            if (contentRight <= contentLeft)
            {
                contentRight = contentLeft + 1;
            }

            if (contentBottom <= contentTop)
            {
                contentBottom = contentTop + 1;
            }

            Rectangle contentBounds = Rectangle.FromLTRB(contentLeft, contentTop, contentRight, contentBottom);
            Rectangle textBounds = contentBounds;

            return new NoteSurfaceLayout(noteBounds, contentBounds, textBounds, headerBounds);
        }

        internal static Rectangle CreateSquarePreviewBounds(Size controlSize, int outerMargin)
        {
            int availableWidth = Math.Max(1, controlSize.Width - (2 * outerMargin));
            int availableHeight = Math.Max(1, controlSize.Height - (2 * outerMargin));
            int squareSize = Math.Max(1, Math.Min(availableWidth, availableHeight));
            int left = outerMargin + ((availableWidth - squareSize) / 2);
            int top = outerMargin;
            return new Rectangle(left, top, squareSize, squareSize);
        }

        internal static Rectangle CreateHeaderButtonBounds(Rectangle noteBounds, AppSettings settings, NoteSurfaceLayoutMetrics metrics, int indexFromRight)
        {
            int decorativeInset = metrics.GetDecorativeInset(settings);
            int innerWidth = Math.Max(1, noteBounds.Width - (2 * decorativeInset));
            int localLeft = NoteHeaderLayout.GetButtonLeft(innerWidth, metrics.HeaderButtonSize, metrics.HeaderRightMargin, metrics.HeaderButtonSpacing, indexFromRight);

            return new Rectangle(
                noteBounds.Left + decorativeInset + localLeft,
                noteBounds.Top + decorativeInset + metrics.HeaderTop,
                metrics.HeaderButtonSize,
                metrics.HeaderButtonSize);
        }
    }
}
