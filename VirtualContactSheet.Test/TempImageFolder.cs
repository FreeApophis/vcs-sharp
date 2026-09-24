namespace VirtualContactSheet.Test;

/// <summary>A throwaway folder of real (tiny) image files, so the image pipeline can be tested end to end.</summary>
internal sealed class TempImageFolder : IDisposable
{
    public TempImageFolder()
        => Root = Directory.CreateTempSubdirectory("vcs-images-").FullName;

    public string Root { get; }

    /// <summary>Writes a solid-colour image and returns its full path.</summary>
    public string Add(
        string name,
        int width = 40,
        int height = 30,
        SKColor? color = null,
        SKEncodedImageFormat format = SKEncodedImageFormat.Png)
    {
        var path = Path.Combine(Root, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var bitmap = TestFrames.Solid(width, height, color ?? SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 95);
        using var file = File.Create(path);
        data.SaveTo(file);

        return path;
    }

    /// <summary>Writes a file that is not an image at all.</summary>
    public string AddNonImage(string name)
    {
        var path = Path.Combine(Root, name);
        File.WriteAllText(path, "not an image");
        return path;
    }

    public void Dispose() => Directory.Delete(Root, recursive: true);
}

/// <summary>An <see cref="IImageLoader"/> that returns solid bitmaps without touching the file system.</summary>
internal sealed class FakeImageLoader : IImageLoader
{
    public int LoadCount { get; private set; }

    public List<string> LoadedPaths { get; } = [];

    public Task<SKBitmap> LoadAsync(string imagePath, int width, int height, CancellationToken ct = default)
    {
        LoadCount++;
        LoadedPaths.Add(imagePath);

        var color = TestFrames.Palette[(LoadCount - 1) % TestFrames.Palette.Length];
        return Task.FromResult(TestFrames.Solid(width, height > 0 ? height : width, color));
    }
}

/// <summary>An <see cref="IImageInfoProvider"/> returning fixed metadata without reading files.</summary>
internal sealed class FakeImageProbe : IImageInfoProvider
{
    private readonly int _width;

    private readonly int _height;

    public FakeImageProbe(int width = 160, int height = 90)
    {
        _width = width;
        _height = height;
    }

    public Task<ImageInfo> ProbeAsync(string path, CancellationToken ct = default)
        => Task.FromResult(new ImageInfo
        {
            Path = path,
            Width = _width,
            Height = _height,
            FileSize = 2048,
            Extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant(),
            Format = "Png",
        });
}
