using SkiaSharp;

namespace VirtualContactSheet;

/// <summary>Layouts that keep every picture's own proportions, so nothing has to be cropped.</summary>
public static class MasonryPlanner
{
    /// <summary>
    /// Justified rows, in reading order. Each row takes pictures until it would come out at
    /// <paramref name="targetHeight"/> or lower when scaled to the full width, closing before or
    /// after the picture that tips it, whichever height is closer to the target; then it is scaled
    /// to end exactly at the right edge. The last row, if it cannot be filled, stays at the target
    /// height and left-aligned rather than being blown up.
    /// </summary>
    /// <param name="aspects">Width ÷ height of each picture, in order.</param>
    /// <param name="width">Width of the body, margins included.</param>
    /// <param name="targetHeight">The picture height rows aim for.</param>
    /// <param name="spacing">Margins, gaps and frames to leave room for.</param>
    public static BodyLayout Rows(IReadOnlyList<double> aspects, int width, int targetHeight, LayoutSpacing spacing)
    {
        int inner = width - (2 * spacing.Margin);
        var pictures = new List<SKRectI>(aspects.Count);
        int top = spacing.Margin;

        foreach (var row in BreakIntoRows(aspects, inner, targetHeight, spacing))
        {
            var rowAspects = aspects.Skip(row.Start).Take(row.Count).ToList();
            double exactHeight = row.Justified ? Available(row.Count, inner, spacing) / rowAspects.Sum() : targetHeight;
            int rowHeight = Math.Max(1, (int)Math.Round(exactHeight));

            var widths = SplitRounded(rowAspects.Select(aspect => aspect * exactHeight));
            int left = spacing.Margin;
            foreach (int pictureWidth in widths)
            {
                pictures.Add(SKRectI.Create(left + spacing.FrameSide, top + spacing.FrameTop, pictureWidth, rowHeight));
                left += pictureWidth + spacing.FrameWidth + spacing.Gap;
            }

            top += rowHeight + spacing.FrameHeight + spacing.Gap;
        }

        int height = pictures.Count > 0 ? top - spacing.Gap + spacing.Margin : 2 * spacing.Margin;
        return new BodyLayout(width, height, pictures);
    }

    /// <summary>
    /// Columns of equal width, like a pin board: each picture goes into the currently shortest
    /// column (the leftmost on a tie), so the order reads roughly left to right, top to bottom.
    /// With <see cref="ColumnEnds.Cropped"/> every column is then scaled to the mean height of the
    /// filled columns; each picture in it grows or shrinks a little, to be cropped to fit.
    /// </summary>
    /// <param name="aspects">Width ÷ height of each picture, in order.</param>
    /// <param name="columns">Number of columns; the body is always as wide as all of them.</param>
    /// <param name="columnWidth">Picture width in every column.</param>
    /// <param name="ends">Whether the columns end evenly or keep their natural heights.</param>
    /// <param name="spacing">Margins, gaps and frames to leave room for.</param>
    public static BodyLayout Columns(IReadOnlyList<double> aspects, int columns, int columnWidth, ColumnEnds ends, LayoutSpacing spacing)
    {
        columns = Math.Max(1, columns);
        columnWidth = Math.Max(1, columnWidth);

        var stacks = Stack(aspects, columns, columnWidth, spacing);
        var filled = stacks.Where(stack => stack.Count > 0).ToList();
        int columnHeight = filled.Count == 0
            ? 0
            : ends switch
            {
                ColumnEnds.Cropped => (int)Math.Round(filled.Average(stack => OuterHeight(stack, spacing))),
                _ => filled.Max(stack => OuterHeight(stack, spacing)),
            };

        var pictures = new SKRectI[aspects.Count];
        for (int column = 0; column < columns; column++)
        {
            var stack = stacks[column];
            var heights = ends == ColumnEnds.Cropped && stack.Count > 0
                ? [.. SplitRounded(Rescaled(stack, columnHeight - (stack.Count * spacing.FrameHeight) - ((stack.Count - 1) * spacing.Gap)))]
                : stack.Select(picture => picture.Height).ToList();

            int left = spacing.Margin + (column * (columnWidth + spacing.FrameWidth + spacing.Gap));
            int top = spacing.Margin;
            foreach (var (picture, height) in stack.Zip(heights))
            {
                pictures[picture.Index] = SKRectI.Create(left + spacing.FrameSide, top + spacing.FrameTop, columnWidth, height);
                top += height + spacing.FrameHeight + spacing.Gap;
            }
        }

        return new BodyLayout(
            spacing.Extent(columns, columnWidth + spacing.FrameWidth),
            (2 * spacing.Margin) + columnHeight,
            pictures);
    }

    /// <summary>Deals the pictures, at their natural heights, onto the shortest column each time.</summary>
    private static List<StackedPicture>[] Stack(IReadOnlyList<double> aspects, int columns, int columnWidth, LayoutSpacing spacing)
    {
        var stacks = Enumerable.Range(0, columns).Select(_ => new List<StackedPicture>()).ToArray();
        for (int index = 0; index < aspects.Count; index++)
        {
            int height = Math.Max(1, (int)Math.Round(columnWidth / aspects[index]));
            var shortest = stacks.MinBy(stack => OuterHeight(stack, spacing))!;
            shortest.Add(new StackedPicture(index, height));
        }

        return stacks;
    }

    /// <summary>Height of a column: its pictures with their frames, and a gap between each pair.</summary>
    private static int OuterHeight(IReadOnlyList<StackedPicture> stack, LayoutSpacing spacing)
        => stack.Count == 0 ? 0 : stack.Sum(picture => picture.Height + spacing.FrameHeight) + ((stack.Count - 1) * spacing.Gap);

    /// <summary>The pictures' heights scaled so that together they take up <paramref name="available"/>.</summary>
    private static IEnumerable<double> Rescaled(IReadOnlyList<StackedPicture> stack, int available)
    {
        double scale = available / (double)stack.Sum(picture => picture.Height);
        return stack.Select(picture => picture.Height * scale);
    }

    /// <summary>Picture width left in a row of <paramref name="count"/> once frames and gaps are taken.</summary>
    private static double Available(int count, int inner, LayoutSpacing spacing)
        => inner - (count * spacing.FrameWidth) - ((count - 1) * spacing.Gap);

    private static IEnumerable<RowBreak> BreakIntoRows(IReadOnlyList<double> aspects, int inner, int targetHeight, LayoutSpacing spacing)
    {
        int start = 0;
        while (start < aspects.Count)
        {
            var row = NextRow(aspects, start, inner, targetHeight, spacing);
            yield return row;
            start += row.Count;
        }
    }

    private static RowBreak NextRow(IReadOnlyList<double> aspects, int start, int inner, int targetHeight, LayoutSpacing spacing)
    {
        double aspectSum = 0;
        double previousHeight = double.PositiveInfinity;
        for (int count = 1; start + count <= aspects.Count; count++)
        {
            aspectSum += aspects[start + count - 1];
            double height = Available(count, inner, spacing) / aspectSum;

            if (height <= targetHeight)
            {
                bool closerWithout = count > 1 && previousHeight - targetHeight < targetHeight - height;
                return new RowBreak(start, closerWithout ? count - 1 : count, Justified: true);
            }

            previousHeight = height;
        }

        return new RowBreak(start, aspects.Count - start, Justified: false);
    }

    /// <summary>
    /// Rounds a run of lengths to whole pixels so that they add up to the rounded total: each
    /// length is the step between rounded running sums, so no rounding error accumulates.
    /// </summary>
    private static IEnumerable<int> SplitRounded(IEnumerable<double> lengths)
    {
        double sum = 0;
        int previousEdge = 0;
        foreach (double length in lengths)
        {
            sum += length;
            int edge = (int)Math.Round(sum);
            yield return Math.Max(1, edge - previousEdge);
            previousEdge = edge;
        }
    }

    private sealed record RowBreak(int Start, int Count, bool Justified);

    private sealed record StackedPicture(int Index, int Height);
}
