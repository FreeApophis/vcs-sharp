namespace VirtualContactSheet.Test;

public class VideoStreamTests
{
    [Fact]
    public void AspectRatio_AnamorphicStream_IsDisplayRatioOverStorageRatio()
    {
        var stream = new VideoStream { Width = 720, Height = 480, DisplayAspectRatio = "16:9" };

        Assert.Equal((16.0 / 9.0) / (720.0 / 480.0), stream.AspectRatio, precision: 10);
    }

    [Fact]
    public void AspectRatio_SquarePixels_IsOne()
    {
        var stream = new VideoStream { Width = 1920, Height = 1080, DisplayAspectRatio = "16:9" };

        Assert.Equal(1.0, stream.AspectRatio, precision: 10);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("16/9")]
    [InlineData("16:9:1")]
    [InlineData("a:9")]
    [InlineData("16:0")]
    public void AspectRatio_WithoutUsableDisplayAspectRatio_IsOne(string? displayAspectRatio)
    {
        var stream = new VideoStream { Width = 720, Height = 480, DisplayAspectRatio = displayAspectRatio };

        Assert.Equal(1.0, stream.AspectRatio);
    }

    [Fact]
    public void AspectRatio_WithUnknownDimensions_IsOne()
    {
        var stream = new VideoStream { DisplayAspectRatio = "16:9" };

        Assert.Equal(1.0, stream.AspectRatio);
    }

    [Fact]
    public void AspectRatio_WithZeroDimensions_IsOne()
    {
        var stream = new VideoStream { Width = 0, Height = 480, DisplayAspectRatio = "16:9" };

        Assert.Equal(1.0, stream.AspectRatio);
    }
}
