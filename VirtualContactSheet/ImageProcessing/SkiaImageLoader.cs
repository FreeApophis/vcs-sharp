using SkiaSharp;

namespace VirtualContactSheet.ImageProcessing;

/// <summary>
/// Decodes image files with SkiaSharp and rescales them to the exact cell size the grid needs,
/// straightening EXIF-rotated photos on the way. Every thumbnail comes back at the same size,
/// which is what <see cref="ContactSheet"/> lays the grid out from.
/// </summary>
public sealed class SkiaImageLoader : IImageLoader
{
    private static readonly SKSamplingOptions Sampling = new(SKCubicResampler.Mitchell);

    private readonly ImageFit _fit;

    private readonly SKColor _background;

    /// <param name="fit">How an image whose aspect ratio differs from the cell is fitted.</param>
    /// <param name="background">Letterbox fill for <see cref="ImageFit.Contain"/>; transparent by
    /// default, so the sheet background shows through.</param>
    public SkiaImageLoader(ImageFit fit = ImageFit.Contain, SKColor? background = null)
    {
        _fit = fit;
        _background = background ?? SKColors.Transparent;
    }

    public Task<SKBitmap> LoadAsync(string imagePath, int width, int height, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        using var codec = SKCodec.Create(imagePath)
            ?? throw new CaptureException($"Unsupported or corrupt image: '{imagePath}'.");

        using var decoded = SKBitmap.Decode(codec)
            ?? throw new CaptureException($"Failed to decode image: '{imagePath}'.");

        var origin = codec.EncodedOrigin;
        if (origin is SKEncodedOrigin.Default or SKEncodedOrigin.TopLeft)
        {
            return Task.FromResult(ScaleToCell(decoded, width, height));
        }

        using var upright = Straighten(decoded, origin);
        return Task.FromResult(ScaleToCell(upright, width, height));
    }

    private SKBitmap ScaleToCell(SKBitmap source, int width, int height)
    {
        var (targetWidth, targetHeight) = TargetSize(source.Width, source.Height, width, height);
        return Scale(source, targetWidth, targetHeight);
    }

    /// <summary>Resolves the cell size: a missing dimension is derived from the image's aspect ratio.</summary>
    private static (int Width, int Height) TargetSize(int sourceWidth, int sourceHeight, int width, int height)
    {
        double aspect = sourceHeight > 0 ? sourceWidth / (double)sourceHeight : 1.0;

        return (width > 0, height > 0) switch
        {
            (true, true) => (width, height),
            (true, false) => (width, Math.Max(1, (int)Math.Round(width / aspect))),
            (false, true) => (Math.Max(1, (int)Math.Round(height * aspect)), height),
            _ => (sourceWidth, sourceHeight),
        };
    }

    private SKBitmap Scale(SKBitmap source, int width, int height)
    {
        var target = new SKBitmap(width, height, source.ColorType, source.AlphaType);

        using var canvas = new SKCanvas(target);
        canvas.Clear(_background);

        var (sourceRect, destinationRect) = Layout(source.Width, source.Height, width, height, _fit);
        canvas.DrawBitmap(source, sourceRect, destinationRect, Sampling);

        return target;
    }

    /// <summary>Source crop and destination rectangle implementing the three <see cref="ImageFit"/> modes.</summary>
    private static (SKRect Source, SKRect Destination) Layout(int sourceWidth, int sourceHeight, int width, int height, ImageFit fit)
    {
        var whole = SKRect.Create(sourceWidth, sourceHeight);
        var cell = SKRect.Create(width, height);

        switch (fit)
        {
            case ImageFit.Stretch:
                return (whole, cell);

            case ImageFit.Cover:
            {
                float scale = Math.Max(width / (float)sourceWidth, height / (float)sourceHeight);
                float cropWidth = width / scale;
                float cropHeight = height / scale;
                return (SKRect.Create((sourceWidth - cropWidth) / 2f, (sourceHeight - cropHeight) / 2f, cropWidth, cropHeight), cell);
            }

            default:
            {
                float scale = Math.Min(width / (float)sourceWidth, height / (float)sourceHeight);
                float drawWidth = sourceWidth * scale;
                float drawHeight = sourceHeight * scale;
                return (whole, SKRect.Create((width - drawWidth) / 2f, (height - drawHeight) / 2f, drawWidth, drawHeight));
            }
        }
    }

    /// <summary>Redraws the pixels upright according to the EXIF orientation.</summary>
    private static SKBitmap Straighten(SKBitmap source, SKEncodedOrigin origin)
    {
        var (width, height) = ImageOrientation.Upright(origin, source.Width, source.Height);
        var target = new SKBitmap(width, height, source.ColorType, source.AlphaType);

        using var canvas = new SKCanvas(target);
        canvas.SetMatrix(ImageOrientation.Matrix(origin, source.Width, source.Height));
        canvas.DrawBitmap(source, 0, 0, Sampling);

        return target;
    }
}
