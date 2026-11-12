using SkiaSharp;

namespace VirtualContactSheet;

/// <summary>
/// Where the pictures go on the body of the sheet (the part below the header), relative to its
/// top-left corner. A pure description: no pixels involved, so planners are easy to test.
/// </summary>
public sealed record BodyLayout(int Width, int Height, IReadOnlyList<SKRectI> Pictures);
