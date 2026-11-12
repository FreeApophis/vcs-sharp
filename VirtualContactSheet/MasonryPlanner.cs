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
}
