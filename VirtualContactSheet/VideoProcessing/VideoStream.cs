namespace VirtualContactSheet.VideoProcessing;

public sealed class VideoStream
{
    public int Index { get; init; }

    public string? Codec { get; init; }

    public string? Profile { get; init; }

    public Option<int> Width { get; init; }

    public Option<int> Height { get; init; }

    public string? PixelFormat { get; init; }

    public long? BitRate { get; init; }

    public double? FrameRate { get; init; }

    public string? DisplayAspectRatio { get; init; }

    /// <summary>Pixel aspect ratio derived from DAR/resolution; 1.0 for square pixels.</summary>
    public double AspectRatio
        => (from width in Width.Where(IsPositive)
            from height in Height.Where(IsPositive)
            from displayRatio in ParseRatio(DisplayAspectRatio)
            select displayRatio / ((double)width / height))
           .GetOrElse(1.0);

    private static bool IsPositive(int value) => value > 0;

    /// <summary>"16:9" → 1.777…; None for anything that is not two numbers with a non-zero divisor.</summary>
    private static Option<double> ParseRatio(string? ratio)
        => ratio?.Split(':') is [var numerator, var denominator]
            ? from n in numerator.ParseDoubleOrNone()
              from d in denominator.ParseDoubleOrNone().Where(d => d != 0)
              select n / d
            : Option<double>.None;
}
