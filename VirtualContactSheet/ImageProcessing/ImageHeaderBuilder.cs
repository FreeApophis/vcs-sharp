namespace VirtualContactSheet.ImageProcessing;

/// <summary>
/// Builds the two-column metadata header for an image collection, mirroring the video header:
/// left column = source / image count / total size, right column = dimensions / formats.
/// </summary>
public static class ImageHeaderBuilder
{
    private const int MaxFormats = 3;

    public static HeaderColumns Build(ImageCollectionInfo info)
        => new(LeftColumn(info), RightColumn(info));

    private static List<string> LeftColumn(ImageCollectionInfo info)
        =>
        [
            info.Source is { } source
                ? $"Folder: {Path.GetFileName(Path.TrimEndingDirectorySeparator(source))}"
                : "Source: file list",
            $"Images: {info.Count}",
            $"Total size: {info.TotalSize.FormatBytes()}",
        ];

    private static List<string> RightColumn(ImageCollectionInfo info)
    {
        var right = new List<string>();

        if (Dimensions(info) is { } dimensions)
        {
            right.Add($"Dimensions: {dimensions}");
        }

        if (Formats(info) is { } formats)
        {
            right.Add($"Format: {formats}");
        }

        return right;
    }

    /// <summary>A single "WxH" when every image agrees, otherwise the smallest and largest by area.</summary>
    private static string? Dimensions(ImageCollectionInfo info)
    {
        var sized = info.Images.Where(image => image is { Width: > 0, Height: > 0 }).ToList();
        if (sized.Count == 0)
        {
            return null;
        }

        var smallest = sized.MinBy(image => (long)image.Width * image.Height)!;
        var largest = sized.MaxBy(image => (long)image.Width * image.Height)!;

        return smallest.Width == largest.Width && smallest.Height == largest.Height
            ? $"{largest.Width}x{largest.Height}"
            : $"{smallest.Width}x{smallest.Height} – {largest.Width}x{largest.Height}";
    }

    /// <summary>"jpg / png", capped so a mixed folder cannot blow up the header line.</summary>
    private static string? Formats(ImageCollectionInfo info)
    {
        var extensions = info.Images
            .Select(image => image.Extension)
            .Where(extension => !string.IsNullOrEmpty(extension))
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();

        return extensions.Count switch
        {
            0 => null,
            <= MaxFormats => string.Join(" / ", extensions),
            _ => $"{string.Join(" / ", extensions.Take(MaxFormats))} / +{extensions.Count - MaxFormats}",
        };
    }
}
