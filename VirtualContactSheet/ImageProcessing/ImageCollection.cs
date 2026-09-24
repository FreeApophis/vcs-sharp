using SkiaSharp;

namespace VirtualContactSheet.ImageProcessing;

/// <summary>
/// Top-level entry point for image contact sheets — the counterpart of Video. Wraps a set of
/// image files, either every supported image in a folder or an explicit list, and composes
/// them into a grid.
/// </summary>
public sealed class ImageCollection
{
    /// <summary>Extensions <see cref="FromFolder"/> picks up, matching what SkiaSharp can decode.</summary>
    public static IReadOnlyList<string> SupportedExtensions { get; } =
        [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".ico", ".wbmp", ".heic", ".heif", ".avif", ".dng"];

    private readonly IImageLoader? _loader;

    private readonly IImageInfoProvider _probe;

    private ImageCollectionInfo? _info;

    /// <param name="paths">The image files, in sheet order.</param>
    /// <param name="loader">Decoder/scaler for the thumbnails. Defaults to a
    /// <see cref="SkiaImageLoader"/> that letterboxes with the sheet background.</param>
    /// <param name="probe">Metadata provider. Defaults to <see cref="SkiaImageInfoProvider"/>.</param>
    /// <param name="source">The folder the paths came from, shown in the header.</param>
    public ImageCollection(
        IEnumerable<string> paths,
        IImageLoader? loader = null,
        IImageInfoProvider? probe = null,
        string? source = null)
    {
        ArgumentNullException.ThrowIfNull(paths);
        Paths = [.. paths];
        Source = source;
        _loader = loader;
        _probe = probe ?? new SkiaImageInfoProvider();
    }

    /// <summary>The image files, in sheet order.</summary>
    public IReadOnlyList<string> Paths { get; }

    /// <summary>The folder this collection was built from, or <c>null</c> for an explicit file list.</summary>
    public string? Source { get; }

    public int Count => Paths.Count;

    /// <summary>What happens when the collection holds more images than the grid has cells.</summary>
    public ImageSelection Selection { get; set; } = ImageSelection.Sample;

    /// <summary>
    /// Caption drawn on each thumbnail when <see cref="ContactSheetOptions.Timestamp"/> is on.
    /// Defaults to the file name; return <c>null</c> to leave a thumbnail uncaptioned.
    /// </summary>
    public Func<ImageInfo, string?> Caption { get; set; } = image => Path.GetFileName(image.Path);

    /// <summary>Every supported image in <paramref name="folder"/>, ordered by file name.</summary>
    public static ImageCollection FromFolder(
        string folder,
        bool recursive = false,
        IImageLoader? loader = null,
        IImageInfoProvider? probe = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        if (!Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException($"Image folder not found: '{folder}'.");
        }

        var files = Directory
            .EnumerateFiles(folder, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Where(IsSupported)
            .Order(StringComparer.OrdinalIgnoreCase);

        return new ImageCollection(files, loader, probe, folder);
    }

    /// <summary>An explicit list of images, kept in the order given.</summary>
    public static ImageCollection FromFiles(
        IEnumerable<string> files,
        IImageLoader? loader = null,
        IImageInfoProvider? probe = null)
        => new(files, loader, probe);

    /// <summary>True when the extension is one <see cref="FromFolder"/> collects.</summary>
    public static bool IsSupported(string path)
        => SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>Probe and cache metadata for every image in the collection.</summary>
    public async Task<ImageCollectionInfo> GetInfoAsync(CancellationToken ct = default)
    {
        if (_info is not null)
        {
            return _info;
        }

        var images = new List<ImageInfo>(Paths.Count);
        foreach (var path in Paths)
        {
            ct.ThrowIfCancellationRequested();
            images.Add(await _probe.ProbeAsync(path, ct).ConfigureAwait(false));
        }

        return _info = new ImageCollectionInfo { Images = images, Source = Source };
    }

    public async Task<bool> IsValidAsync(CancellationToken ct = default)
    {
        try
        {
            var info = await GetInfoAsync(ct).ConfigureAwait(false);
            return info.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Load a single image scaled into a <paramref name="width"/> × <paramref name="height"/> cell.</summary>
    public Task<SKBitmap> LoadThumbnailAsync(string imagePath, int width, int height = 0, CancellationToken ct = default)
        => (_loader ?? new SkiaImageLoader()).LoadAsync(imagePath, width, height, ct);

    /// <summary>
    /// Pick the images that go on the sheet — the image counterpart of computing capture times.
    /// With <see cref="ImageSelection.Sample"/> the picks are spread evenly over the collection.
    /// </summary>
    public IReadOnlyList<ImageInfo> SelectImages(ImageCollectionInfo info, ContactSheetOptions options)
    {
        var images = info.Images;
        int capacity = Math.Max(1, options.Columns * options.Rows);

        if (Selection == ImageSelection.All || images.Count <= capacity)
        {
            return images;
        }

        // Sample at the middle of each segment, exactly like the even frame distribution for video.
        return
        [
            .. Enumerable
                .Range(0, capacity)
                .Select(i => images[(int)((i + 0.5) * images.Count / capacity)]),
        ];
    }

    /// <summary>Build the contact sheet and return encoded image bytes.</summary>
    public async Task<byte[]> BuildContactSheetAsync(
        ContactSheetOptions options,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var info = await GetInfoAsync(ct).ConfigureAwait(false);
        if (info.Count == 0)
        {
            throw new CaptureException("The image collection is empty.");
        }

        var selected = SelectImages(info, options);
        var (width, height) = ThumbnailSize(selected[0], options);

        // Without an explicit loader, letterbox bars take the sheet background, so a portrait
        // photo in a landscape cell blends into the sheet instead of punching a hole in it.
        var loader = _loader ?? new SkiaImageLoader(ImageFit.Contain, options.SheetBackground);
        var thumbs = new List<ContactSheet.Thumbnail>(selected.Count);

        try
        {
            int done = 0;
            foreach (var image in selected)
            {
                ct.ThrowIfCancellationRequested();
                var bitmap = await loader.LoadAsync(image.Path, width, height, ct).ConfigureAwait(false);
                thumbs.Add(new ContactSheet.Thumbnail(bitmap, Caption(image)));
                progress?.Report(++done / (double)selected.Count);
            }

            var sheet = new ContactSheet(options)
            {
                HeaderOverride = ImageHeaderBuilder.Build(info),
            };

            return sheet.Render(thumbs, options.Title);
        }
        finally
        {
            foreach (var thumb in thumbs)
            {
                thumb.Image.Dispose();
            }
        }
    }

    /// <summary>Build a contact sheet and write it to disk.</summary>
    public async Task SaveContactSheetAsync(
        string outputPath,
        ContactSheetOptions options,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var bytes = await BuildContactSheetAsync(options, progress, ct).ConfigureAwait(false);
        await File.WriteAllBytesAsync(outputPath, bytes, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Cell size for every thumbnail: <see cref="ContactSheetOptions.AspectRatio"/> wins, then
    /// <see cref="ContactSheetOptions.ThumbnailHeight"/>, else the first image's own aspect ratio.
    /// </summary>
    private static (int Width, int Height) ThumbnailSize(ImageInfo first, ContactSheetOptions options)
    {
        int width = Math.Max(1, options.ThumbnailWidth);

        double aspect = options.AspectRatio > 0 ? options.AspectRatio : first.AspectRatio;
        int height = options.AspectRatio <= 0 && options.ThumbnailHeight > 0
            ? options.ThumbnailHeight
            : Math.Max(1, (int)Math.Round(width / aspect));

        return (width, height);
    }
}
