using SkiaSharp;

namespace VirtualContactSheet.ImageProcessing;

/// <summary>
/// EXIF orientation support. Decoders hand back the stored pixels as-is, so a photo shot in
/// portrait arrives rotated; these helpers turn an <see cref="SKEncodedOrigin"/> into the
/// upright dimensions and the canvas transform that straightens it.
/// </summary>
internal static class ImageOrientation
{
    /// <summary>True when the origin swaps the width and height axes.</summary>
    public static bool IsTransposed(SKEncodedOrigin origin)
        => origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

    /// <summary>The stored dimensions as they appear after straightening.</summary>
    public static (int Width, int Height) Upright(SKEncodedOrigin origin, int width, int height)
        => IsTransposed(origin) ? (height, width) : (width, height);

    /// <summary>
    /// Maps stored pixel coordinates onto upright ones. Parameters are the <em>stored</em>
    /// dimensions; the matrix is meant for a canvas sized <see cref="Upright"/>.
    /// </summary>
    public static SKMatrix Matrix(SKEncodedOrigin origin, float width, float height) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, height, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, height, -1, 0, width, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, width, 0, 0, 1),
        _ => SKMatrix.Identity,
    };
}
