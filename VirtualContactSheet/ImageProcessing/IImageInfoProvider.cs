namespace VirtualContactSheet.ImageProcessing;

/// <summary>Provides dimensions, format and size for an image file without decoding its pixels.</summary>
public interface IImageInfoProvider
{
    Task<ImageInfo> ProbeAsync(string path, CancellationToken ct = default);
}
