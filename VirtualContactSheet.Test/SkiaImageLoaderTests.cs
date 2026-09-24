namespace VirtualContactSheet.Test;

public class SkiaImageLoaderTests
{
    private static readonly SKColor Ink = new(0xE0, 0x10, 0x10);

    [Fact]
    public async Task Contain_KeepsTheAspectRatioAndLetterboxesTheRest()
    {
        using var folder = new TempImageFolder();
        var path = folder.Add("wide.png", width: 100, height: 50, color: Ink);

        using var thumbnail = await new SkiaImageLoader().LoadAsync(path, 60, 60);

        Assert.Equal(60, thumbnail.Width);
        Assert.Equal(60, thumbnail.Height);

        // 100x50 scaled into a 60x60 cell → a 60x30 band centred vertically.
        Assert.Equal(Ink, thumbnail.GetPixel(30, 30));
        Assert.Equal((byte)0, thumbnail.GetPixel(30, 2).Alpha);
        Assert.Equal((byte)0, thumbnail.GetPixel(30, 57).Alpha);
    }

    [Fact]
    public async Task Contain_WithBackground_FillsTheLetterboxBars()
    {
        using var folder = new TempImageFolder();
        var path = folder.Add("wide.png", width: 100, height: 50, color: Ink);

        using var thumbnail = await new SkiaImageLoader(ImageFit.Contain, SKColors.White).LoadAsync(path, 60, 60);

        Assert.Equal(SKColors.White, thumbnail.GetPixel(30, 2));
    }

    [Fact]
    public async Task Cover_FillsTheWholeCell()
    {
        using var folder = new TempImageFolder();
        var path = folder.Add("wide.png", width: 100, height: 50, color: Ink);

        using var thumbnail = await new SkiaImageLoader(ImageFit.Cover).LoadAsync(path, 60, 60);

        Assert.Equal(Ink, thumbnail.GetPixel(2, 2));
        Assert.Equal(Ink, thumbnail.GetPixel(57, 57));
    }

    [Fact]
    public async Task HeightZero_DerivesTheHeightFromTheImageAspectRatio()
    {
        using var folder = new TempImageFolder();
        var path = folder.Add("wide.png", width: 100, height: 50, color: Ink);

        using var thumbnail = await new SkiaImageLoader().LoadAsync(path, 80, 0);

        Assert.Equal(80, thumbnail.Width);
        Assert.Equal(40, thumbnail.Height);
    }

    [Fact]
    public async Task UnreadableFile_ThrowsCaptureException()
    {
        using var folder = new TempImageFolder();
        var path = folder.AddNonImage("broken.png");

        await Assert.ThrowsAsync<CaptureException>(async () =>
        {
            using var thumbnail = await new SkiaImageLoader().LoadAsync(path, 60, 60);
        });
    }

    [Fact]
    public async Task Probe_ReadsDimensionsFormatAndSize()
    {
        using var folder = new TempImageFolder();
        var path = folder.Add("photo.jpg", width: 120, height: 80, format: SKEncodedImageFormat.Jpeg);

        var info = await new SkiaImageInfoProvider().ProbeAsync(path);

        Assert.Equal(120, info.Width);
        Assert.Equal(80, info.Height);
        Assert.Equal("jpg", info.Extension);
        Assert.Equal("Jpeg", info.Format);
        Assert.Equal(new FileInfo(path).Length, info.FileSize);
        Assert.Equal(1.5, info.AspectRatio, precision: 6);
    }

    [Fact]
    public async Task Probe_MissingFile_Throws()
        => await Assert.ThrowsAsync<FileNotFoundException>(
            () => new SkiaImageInfoProvider().ProbeAsync(Path.Combine(Path.GetTempPath(), "vcs-missing-4b2e.png")));
}
