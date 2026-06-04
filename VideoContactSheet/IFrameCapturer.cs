using SkiaSharp;

namespace VideoContactSheet;

/// <summary>Extracts single frames from a video at a given time index.</summary>
public interface IFrameCapturer
{
    Task<SKBitmap> CaptureAsync(string videoPath, TimeIndex time, int width, int height = 0, CancellationToken ct = default);
}
