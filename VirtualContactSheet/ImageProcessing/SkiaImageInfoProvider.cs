using SkiaSharp;

namespace VirtualContactSheet.ImageProcessing;

/// <summary>Reads image metadata with SkiaSharp's codec layer: header only, no pixel decoding.</summary>
public sealed class SkiaImageInfoProvider : IImageInfoProvider
{
    public Task<ImageInfo> ProbeAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Image file not found.", path);
        }

        using var codec = SKCodec.Create(path)
            ?? throw new CaptureException($"Unsupported or corrupt image: '{path}'.");

        // EXIF may store the pixels rotated; report the upright dimensions the sheet will show.
        var size = ImageOrientation.Upright(codec.EncodedOrigin, codec.Info.Width, codec.Info.Height);

        return Task.FromResult(new ImageInfo
        {
            Path = path,
            Width = size.Width,
            Height = size.Height,
            FileSize = new FileInfo(path).Length,
            Extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant(),
            Format = codec.EncodedFormat.ToString(),
        });
    }
}
