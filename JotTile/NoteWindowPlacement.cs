using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using JotTile.Core;

namespace JotTile
{
    internal static class NoteWindowPlacement
    {
        private const int MinimumWidth = 160;
        private const int MinimumHeight = 90;
        private const int VisibleMargin = 16;

        internal static Rectangle[] GetWorkingAreas()
        {
            return Screen.AllScreens
                .Select(screen => screen.WorkingArea)
                .ToArray();
        }

        internal static Rectangle GetPrimaryWorkingArea()
        {
            if (Screen.PrimaryScreen != null)
            {
                return Screen.PrimaryScreen.WorkingArea;
            }

            Rectangle[] workingAreas = GetWorkingAreas();
            if (workingAreas.Length > 0)
            {
                return workingAreas[0];
            }

            return new Rectangle(0, 0, 1280, 720);
        }

        internal static void NormalizeLoadedNote(NoteRecord note, Rectangle fallbackWorkingArea, Rectangle[] workingAreas)
        {
            if (string.IsNullOrWhiteSpace(note.Id))
            {
                note.Id = Guid.NewGuid().ToString("N");
            }

            if (note.Text == null)
            {
                note.Text = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(note.CreatedAt))
            {
                note.CreatedAt = DateTime.UtcNow.ToString("o");
            }

            if (string.IsNullOrWhiteSpace(note.UpdatedAt))
            {
                note.UpdatedAt = note.CreatedAt;
            }

            note.Width = Math.Max(MinimumWidth, note.Width);
            note.Height = Math.Max(MinimumHeight, note.Height);

            Rectangle clampedBounds = ClampToVisibleArea(
                new Rectangle(note.X, note.Y, note.Width, note.Height),
                fallbackWorkingArea,
                workingAreas);

            note.X = clampedBounds.X;
            note.Y = clampedBounds.Y;
            note.Width = clampedBounds.Width;
            note.Height = clampedBounds.Height;
        }

        internal static Rectangle CreateNewNoteBounds(Rectangle workingArea, int noteCount, Size defaultSize)
        {
            int offset = noteCount * 24;
            Rectangle candidate = new Rectangle(
                workingArea.Left + 80 + offset,
                workingArea.Top + 80 + offset,
                Math.Max(MinimumWidth, defaultSize.Width),
                Math.Max(MinimumHeight, defaultSize.Height));

            return ClampToVisibleArea(candidate, workingArea, new[] { workingArea });
        }

        internal static Rectangle ClampToVisibleArea(Rectangle bounds, Rectangle fallbackWorkingArea, Rectangle[] workingAreas)
        {
            Rectangle[] availableAreas = workingAreas != null && workingAreas.Length > 0
                ? workingAreas
                : new[] { fallbackWorkingArea };

            Rectangle targetArea = ChooseWorkingArea(bounds, fallbackWorkingArea, availableAreas);
            int minX = targetArea.Left + VisibleMargin;
            int minY = targetArea.Top + VisibleMargin;
            int maxX = Math.Max(minX, targetArea.Right - VisibleMargin - bounds.Width);
            int maxY = Math.Max(minY, targetArea.Bottom - VisibleMargin - bounds.Height);

            return new Rectangle(
                Clamp(bounds.X, minX, maxX),
                Clamp(bounds.Y, minY, maxY),
                bounds.Width,
                bounds.Height);
        }

        private static Rectangle ChooseWorkingArea(Rectangle bounds, Rectangle fallbackWorkingArea, Rectangle[] workingAreas)
        {
            Point center = new Point(bounds.Left + (bounds.Width / 2), bounds.Top + (bounds.Height / 2));
            for (int index = 0; index < workingAreas.Length; index++)
            {
                if (workingAreas[index].Contains(center))
                {
                    return workingAreas[index];
                }
            }

            Rectangle? bestArea = null;
            int bestIntersectionArea = 0;
            for (int index = 0; index < workingAreas.Length; index++)
            {
                Rectangle intersection = Rectangle.Intersect(bounds, workingAreas[index]);
                int intersectionArea = Math.Max(0, intersection.Width) * Math.Max(0, intersection.Height);
                if (intersectionArea > bestIntersectionArea)
                {
                    bestIntersectionArea = intersectionArea;
                    bestArea = workingAreas[index];
                }
            }

            return bestArea ?? fallbackWorkingArea;
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
