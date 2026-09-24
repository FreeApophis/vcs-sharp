namespace VirtualContactSheet.ImageProcessing;

/// <summary>Aggregate metadata for an <see cref="ImageCollection"/> — the image counterpart of VideoInfo.</summary>
public sealed class ImageCollectionInfo
{
    public IReadOnlyList<ImageInfo> Images { get; init; } = [];

    /// <summary>The folder the collection was built from, or <c>null</c> for an explicit file list.</summary>
    public string? Source { get; init; }

    public int Count => Images.Count;

    public long TotalSize => Images.Sum(image => image.FileSize);

    /// <summary>First image, or null when the collection is empty.</summary>
    public ImageInfo? First => Images.FirstOrDefault();
}
