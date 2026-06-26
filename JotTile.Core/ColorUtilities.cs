using System;
using System.Drawing;
using System.Globalization;

namespace JotTile.Core
{
    internal static class ColorUtilities
    {
        internal static Color Parse(string value, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            try
            {
                string normalized = value.Trim();
                if (normalized.StartsWith("#", StringComparison.Ordinal))
                {
                    normalized = normalized.Substring(1);
                }

                if (normalized.Length == 6)
                {
                    int rgb = int.Parse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    return Color.FromArgb(
                        (rgb >> 16) & 0xFF,
                        (rgb >> 8) & 0xFF,
                        rgb & 0xFF);
                }
            }
            catch
            {
            }

            return fallback;
        }

        internal static string ToHex(Color color)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "#{0:X2}{1:X2}{2:X2}",
                color.R,
                color.G,
                color.B);
        }
    }
}
