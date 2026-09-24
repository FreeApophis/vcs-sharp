namespace VirtualContactSheet.Test;

public class ImageHeaderBuilderTests
{
    [Fact]
    public void LeftColumn_NamesTheFolderCountsImagesAndSumsTheirSize()
    {
        var header = ImageHeaderBuilder.Build(Info(
            source: Path.Combine("C:", "photos", "holiday"),
            images: [Image("a.jpg", 1920, 1080, 1024), Image("b.jpg", 1920, 1080, 1024)]));

        Assert.Equal(["Folder: holiday", "Images: 2", "Total size: 2 KiB"], header.Left);
    }

    [Fact]
    public void LeftColumn_WithoutAFolder_SaysFileList()
    {
        var header = ImageHeaderBuilder.Build(Info(source: null, images: [Image("a.jpg", 640, 480, 100)]));

        Assert.Equal("Source: file list", header.Left[0]);
    }

    [Fact]
    public void RightColumn_UniformImages_ShowsASingleDimension()
    {
        var header = ImageHeaderBuilder.Build(Info(
            images: [Image("a.jpg", 1920, 1080, 1), Image("b.jpg", 1920, 1080, 1)]));

        Assert.Equal("Dimensions: 1920x1080", header.Right[0]);
    }

    [Fact]
    public void RightColumn_MixedImages_ShowsTheSmallestAndLargest()
    {
        var header = ImageHeaderBuilder.Build(Info(
            images: [Image("a.jpg", 1920, 1080, 1), Image("b.png", 640, 480, 1), Image("c.jpg", 800, 600, 1)]));

        Assert.Equal("Dimensions: 640x480 – 1920x1080", header.Right[0]);
        Assert.Equal("Format: jpg / png", header.Right[1]);
    }

    [Fact]
    public void RightColumn_ManyFormats_IsCapped()
    {
        var header = ImageHeaderBuilder.Build(Info(images:
        [
            Image("a.jpg", 10, 10, 1),
            Image("b.png", 10, 10, 1),
            Image("c.gif", 10, 10, 1),
            Image("d.webp", 10, 10, 1),
            Image("e.bmp", 10, 10, 1),
        ]));

        Assert.Equal("Format: bmp / gif / jpg / +2", header.Right[1]);
    }

    [Fact]
    public void EmptyCollection_HasNoRightColumn()
    {
        var header = ImageHeaderBuilder.Build(Info(images: []));

        Assert.Empty(header.Right);
        Assert.Equal("Images: 0", header.Left[1]);
    }

    private static ImageCollectionInfo Info(IReadOnlyList<ImageInfo> images, string? source = null)
        => new() { Images = images, Source = source };

    private static ImageInfo Image(string name, int width, int height, long size)
        => new()
        {
            Path = name,
            Width = width,
            Height = height,
            FileSize = size,
            Extension = Path.GetExtension(name).TrimStart('.'),
        };
}
