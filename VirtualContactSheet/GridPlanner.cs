using SkiaSharp;

namespace VirtualContactSheet;

/// <summary>The regular grid: equally sized pictures, filled row by row.</summary>
public static class GridPlanner
{
    /// <remarks>The width always spans every column, even when the last row is not full.</remarks>
    public static BodyLayout Plan(int count, int columns, SKSizeI picture, LayoutSpacing spacing)
    {
        columns = Math.Max(1, columns);
        int rows = (count + columns - 1) / columns;
        int cellWidth = picture.Width + spacing.FrameWidth;
        int cellHeight = picture.Height + spacing.FrameHeight;

        return new BodyLayout(
            Width: Extent(columns, cellWidth, spacing),
            Height: Extent(rows, cellHeight, spacing),
            Pictures:
            [
                .. Enumerable.Range(0, count).Select(i => SKRectI.Create(
                    spacing.Margin + ((i % columns) * (cellWidth + spacing.Gap)) + spacing.FrameSide,
                    spacing.Margin + ((i / columns) * (cellHeight + spacing.Gap)) + spacing.FrameTop,
                    picture.Width,
                    picture.Height)),
            ]);
    }

    /// <summary>Margins plus <paramref name="cells"/> cells with a gap between each pair.</summary>
    private static int Extent(int cells, int cellSize, LayoutSpacing spacing)
        => (2 * spacing.Margin) + (cells * cellSize) + ((cells - 1) * spacing.Gap);
}
