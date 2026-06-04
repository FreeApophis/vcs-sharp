namespace VideoContactSheet;

public sealed class VideoStream
{
    public int Index { get; init; }

    public string? Codec { get; init; }

    public string? Profile { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public string? PixelFormat { get; init; }

    public long? BitRate { get; init; }

    public double? FrameRate { get; init; }

    public string? DisplayAspectRatio { get; init; }

    /// <summary>Pixel aspect ratio derived from DAR/resolution; 1.0 for square pixels.</summary>
    public double AspectRatio
    {
        get
        {
            if (Width <= 0 || Height <= 0)
            {
                return 1.0;
            }

            if (!string.IsNullOrEmpty(DisplayAspectRatio) && DisplayAspectRatio.Contains(':'))
            {
                var parts = DisplayAspectRatio.Split(':');
                if (parts.Length == 2
                    && double.TryParse(parts[0], out var dw)
                    && double.TryParse(parts[1], out var dh)
                    && dh != 0)
                {
                    return (dw / dh) / ((double)Width / Height);
                }
            }

            return 1.0;
        }
    }
}
