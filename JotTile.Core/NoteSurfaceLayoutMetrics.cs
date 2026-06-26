namespace JotTile.Core
{
    internal sealed class NoteSurfaceLayoutMetrics
    {
        internal int HeaderTop { get; set; }

        internal int HeaderButtonSize { get; set; }

        internal int HeaderButtonSpacing { get; set; }

        internal int HeaderRightMargin { get; set; }

        internal int HeaderBottomGap { get; set; }

        internal int ContentSidePadding { get; set; }

        internal int ContentBottomPadding { get; set; }

        internal int PreviewOuterMargin { get; set; }

        internal int GetDecorativeInset(AppSettings settings)
        {
            return settings.OuterStrokeThickness + settings.FrameThickness + settings.InnerStrokeThickness;
        }

        internal int GetHorizontalChrome(AppSettings settings)
        {
            return 2 * (GetDecorativeInset(settings) + ContentSidePadding);
        }

        internal int GetVerticalChrome(AppSettings settings)
        {
            return (2 * GetDecorativeInset(settings)) + HeaderTop + HeaderButtonSize + HeaderBottomGap + ContentBottomPadding;
        }
    }
}
