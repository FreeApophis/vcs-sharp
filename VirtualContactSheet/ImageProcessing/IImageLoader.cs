using SkiaSharp;

namespace VirtualContactSheet.ImageProcessing;

/// <summary>Decodes an image file and scales it to the exact thumbnail size the grid expects.</summary>
public interface IImageLoader
{
    Task<SKBitmap> LoadAsync(string imagePath, int width, int height, CancellationToken ct = default);
}
