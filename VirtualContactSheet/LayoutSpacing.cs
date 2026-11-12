namespace VirtualContactSheet;

/// <summary>Space around and between the pictures, and the polaroid frame each picture sits in.</summary>
/// <param name="Gap">Space between two neighbouring cells.</param>
/// <param name="Margin">Space between the outermost cells and the edge of the body.</param>
/// <param name="FrameSide">Frame to the left and to the right of each picture.</param>
/// <param name="FrameTop">Frame above each picture.</param>
/// <param name="FrameBottom">Frame below each picture.</param>
public sealed record LayoutSpacing(int Gap, int Margin, int FrameSide, int FrameTop, int FrameBottom)
{
    private const int PolaroidSide = 12;

    private const int PolaroidBottom = 36;

    /// <summary>Frame width added to every picture, left plus right.</summary>
    public int FrameWidth => 2 * FrameSide;

    /// <summary>Frame height added to every picture, top plus bottom.</summary>
    public int FrameHeight => FrameTop + FrameBottom;

    /// <summary>Length of a line of <paramref name="cells"/> cells: margins, the cells, and a gap between each pair.</summary>
    public int Extent(int cells, int cellSize)
        => (2 * Margin) + (cells * cellSize) + ((cells - 1) * Gap);

    /// <summary>The spacing a sheet with these options needs. The margin leaves room for the drop shadow.</summary>
    public static LayoutSpacing For(ContactSheetOptions options)
    {
        int padding = Math.Max(0, options.Padding);
        int shadow = options.SoftShadow ? Math.Max(0, options.ShadowSize) : 0;
        int side = options.Polaroid ? PolaroidSide : 0;
        int bottom = options.Polaroid ? PolaroidBottom : 0;

        return new LayoutSpacing(Gap: padding, Margin: padding + shadow, FrameSide: side, FrameTop: side, FrameBottom: bottom);
    }
}
