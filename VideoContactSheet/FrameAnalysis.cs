using SkiaSharp;

namespace VideoContactSheet;

/// <summary>Utility to measure average brightness, used for blank-frame evasion.</summary>
public static class FrameAnalysis
{
    private const int SampleStep = 8;
    private const double RedWeight = 0.2126;
    private const double GreenWeight = 0.7152;
    private const double BlueWeight = 0.0722;
    private const double NormalizationFactor = 255.0;

    /// <summary>Mean luma in the 0..1 range.</summary>
    public static double AverageBrightness(SKBitmap bitmap)
        => GetSampledPixels(bitmap)
            .Average(Luminosity);

    // Sample on a grid for speed instead of every pixel.
    private static IEnumerable<SKColor> GetSampledPixels(SKBitmap bitmap)
        => from y in Enumerable.Range(0, bitmap.Height / SampleStep)
           from x in Enumerable.Range(0, bitmap.Width / SampleStep)
           select bitmap.GetPixel(x, y);

    private static double Luminosity(SKColor color)
        => ((RedWeight * color.Red) + (GreenWeight * color.Green) + (BlueWeight * color.Blue)) / NormalizationFactor;
}
