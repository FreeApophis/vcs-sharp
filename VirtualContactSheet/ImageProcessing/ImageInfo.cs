namespace VirtualContactSheet.ImageProcessing;

/// <summary>Header-level metadata for a single image file, read without decoding the pixels.</summary>
public sealed class ImageInfo
{
    public string Path { get; init; } = string.Empty;

    /// <summary>Width in pixels, already corrected for the EXIF orientation.</summary>
    public int Width { get; init; }

    /// <summary>Height in pixels, already corrected for the EXIF orientation.</summary>
    public int Height { get; init; }

    public long FileSize { get; init; }

    /// <summary>Lower-case file extension without the leading dot ("jpg", "png").</summary>
    public string Extension { get; init; } = string.Empty;

    /// <summary>Encoded container format as reported by the decoder ("Jpeg", "Png", …).</summary>
    public string? Format { get; init; }

    /// <summary>Width ÷ height; 1.0 when the dimensions are unknown.</summary>
    public double AspectRatio => Width > 0 && Height > 0 ? Width / (double)Height : 1.0;
}
