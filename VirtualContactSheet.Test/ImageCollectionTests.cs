namespace VirtualContactSheet.Test;

public class ImageCollectionTests
{
    [Fact]
    public void FromFolder_TakesSupportedImagesInNameOrder_AndSkipsOtherFiles()
    {
        using var folder = new TempImageFolder();
        folder.Add("b.png");
        folder.Add("a.jpg", format: SKEncodedImageFormat.Jpeg);
        folder.AddNonImage("readme.txt");
        folder.Add(Path.Combine("nested", "c.png"));

        var collection = ImageCollection.FromFolder(folder.Root);

        Assert.Equal(["a.jpg", "b.png"], collection.Paths.Select(Path.GetFileName));
        Assert.Equal(folder.Root, collection.Source);
    }

    [Fact]
    public void FromFolder_Recursive_IncludesSubdirectories()
    {
        using var folder = new TempImageFolder();
        folder.Add("a.png");
        folder.Add(Path.Combine("nested", "c.png"));

        var collection = ImageCollection.FromFolder(folder.Root, recursive: true);

        Assert.Equal(2, collection.Count);
        Assert.Contains(collection.Paths, path => Path.GetFileName(path) == "c.png");
    }

    [Fact]
    public void FromFolder_MissingFolder_Throws()
        => Assert.Throws<DirectoryNotFoundException>(
            () => ImageCollection.FromFolder(Path.Combine(Path.GetTempPath(), "vcs-does-not-exist-9a7f")));

    [Fact]
    public void FromFiles_KeepsTheGivenOrder()
    {
        var collection = ImageCollection.FromFiles(["z.png", "a.png"], new FakeImageLoader(), new FakeImageProbe());

        Assert.Equal(["z.png", "a.png"], collection.Paths);
        Assert.Null(collection.Source);
    }

    [Fact]
    public async Task SelectImages_MoreImagesThanCells_SamplesEvenlyAcrossTheCollection()
    {
        var collection = Fake(Enumerable.Range(0, 100).Select(i => $"{i:D3}.png"));
        var info = await collection.GetInfoAsync();

        var selected = collection.SelectImages(info, new ContactSheetOptions { Columns = 2, Rows = 2 });

        // Midpoints of four equal segments of 100 images: 12, 37, 62, 87.
        Assert.Equal(["012.png", "037.png", "062.png", "087.png"], selected.Select(image => Path.GetFileName(image.Path)));
    }

    [Fact]
    public async Task SelectImages_FewerImagesThanCells_KeepsThemAll()
    {
        var collection = Fake(["a.png", "b.png", "c.png"]);
        var info = await collection.GetInfoAsync();

        var selected = collection.SelectImages(info, new ContactSheetOptions { Columns = 4, Rows = 4 });

        Assert.Equal(3, selected.Count);
    }

    [Fact]
    public async Task SelectImages_AllMode_IgnoresTheGridCapacity()
    {
        var collection = Fake(Enumerable.Range(0, 20).Select(i => $"{i:D2}.png"));
        collection.Selection = ImageSelection.All;
        var info = await collection.GetInfoAsync();

        var selected = collection.SelectImages(info, new ContactSheetOptions { Columns = 2, Rows = 2 });

        Assert.Equal(20, selected.Count);
    }

    [Fact]
    public async Task BuildContactSheet_LaysOutOneCellPerSelectedImage()
    {
        var loader = new FakeImageLoader();
        var collection = new ImageCollection(["a.png", "b.png", "c.png", "d.png"], loader, new FakeImageProbe());

        var bytes = await collection.BuildContactSheetAsync(TextFreeOptions());
        using var decoded = SKBitmap.Decode(bytes);

        Assert.Equal(4, loader.LoadCount);

        // width = 2*160 + 3*2 ; height = 2 rows of (90 + 2) + 2 — same geometry as the video sheet.
        Assert.Equal(326, decoded.Width);
        Assert.Equal(186, decoded.Height);
    }

    [Fact]
    public async Task BuildContactSheet_DerivesThumbnailHeightFromTheFirstImageAspectRatio()
    {
        var loader = new FakeImageLoader();
        var collection = new ImageCollection(["a.png"], loader, new FakeImageProbe(width: 1000, height: 500));

        var options = TextFreeOptions();
        options.Columns = 1;
        options.Rows = 1;
        var bytes = await collection.BuildContactSheetAsync(options);
        using var decoded = SKBitmap.Decode(bytes);

        // 160 wide at 2:1 → 80 high, plus the 2px margin on each side.
        Assert.Equal(164, decoded.Width);
        Assert.Equal(84, decoded.Height);
    }

    [Fact]
    public async Task BuildContactSheet_ReportsProgressOncePerImage()
    {
        var collection = Fake(["a.png", "b.png", "c.png", "d.png"]);

        // A synchronous IProgress, not Progress<T>: the latter posts to the synchronization
        // context, so reports can still be in flight when the build returns. That is an artifact
        // of the reporting type, not of the library, and waiting for them made this test flaky
        // on a loaded runner.
        var reported = new RecordingProgress();

        await collection.BuildContactSheetAsync(TextFreeOptions(), reported);

        Assert.Equal(4, reported.Values.Count);
        Assert.Equal([0.25, 0.5, 0.75, 1.0], reported.Values);
    }

    [Fact]
    public async Task BuildContactSheet_EmptyCollection_Throws()
    {
        var collection = Fake([]);

        await Assert.ThrowsAsync<CaptureException>(() => collection.BuildContactSheetAsync(TextFreeOptions()));
    }

    [Fact]
    public async Task SaveContactSheet_WritesTheFile()
    {
        using var folder = new TempImageFolder();
        var collection = Fake(["a.png", "b.png"]);
        var output = Path.Combine(folder.Root, "sheet.png");

        await collection.SaveContactSheetAsync(output, TextFreeOptions());

        Assert.True(File.Exists(output));
        using var decoded = SKBitmap.Decode(output);
        Assert.NotNull(decoded);
    }

    [Fact]
    public async Task GetInfoAsync_ProbesOnceAndAggregates()
    {
        using var folder = new TempImageFolder();
        folder.Add("a.png", width: 40, height: 20);
        folder.Add("b.png", width: 60, height: 30);

        var collection = ImageCollection.FromFolder(folder.Root);
        var info = await collection.GetInfoAsync();

        Assert.Equal(2, info.Count);
        Assert.Equal(folder.Root, info.Source);
        Assert.Equal([40, 60], info.Images.Select(image => image.Width));
        Assert.True(info.TotalSize > 0);
        Assert.Same(info, await collection.GetInfoAsync());
    }

    [Fact]
    public async Task IsValidAsync_FalseForAFolderWithoutImages()
    {
        using var folder = new TempImageFolder();
        folder.AddNonImage("readme.txt");

        var collection = ImageCollection.FromFolder(folder.Root);

        Assert.False(await collection.IsValidAsync());
    }

    [Fact]
    public async Task IsValidAsync_FalseWhenAFileIsNotDecodable()
    {
        using var folder = new TempImageFolder();
        var broken = folder.AddNonImage("broken.png");

        var collection = ImageCollection.FromFiles([broken]);

        Assert.False(await collection.IsValidAsync());
    }

    [Fact]
    public async Task Caption_DefaultsToTheFileNameAndIsCustomisable()
    {
        var collection = Fake(["folder/holiday.png"]);
        var info = await collection.GetInfoAsync();

        Assert.Equal("holiday.png", collection.Caption(info.Images[0]));

        collection.Caption = image => $"{image.Width}x{image.Height}";
        Assert.Equal("160x90", collection.Caption(info.Images[0]));
    }

    private static ImageCollection Fake(IEnumerable<string> paths)
        => new(paths, new FakeImageLoader(), new FakeImageProbe());

    private static ContactSheetOptions TextFreeOptions() => new()
    {
        Columns = 2,
        Rows = 2,
        Padding = 2,
        ThumbnailWidth = 160,
        SoftShadow = false,
        Polaroid = false,
        Timestamp = false,
        ShowHeader = false,
        ShowSignature = false,
        Title = null,
    };
}
