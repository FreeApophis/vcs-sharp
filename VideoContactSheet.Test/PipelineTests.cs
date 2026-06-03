namespace VideoContactSheet.Test;

public class PipelineTests
{
    [Fact]
    public async Task BuildContactSheet_WithFakes_CapturesGridAndRenders()
    {
        var capturer = new FakeCapturer();
        var video = new Video("fake.mkv", capturer, new FakeProbe());

        var bytes = await video.BuildContactSheetAsync(TextFreeOptions());
        using var decoded = SKBitmap.Decode(bytes);

        // Columns*Rows = 4 frames, one capture each (blank evasion disabled).
        Assert.Equal(4, capturer.CaptureCount);

        // width = 2*160 + 3*2 ; height = 2 rows of (90 + 2) + 2
        Assert.Equal(326, decoded.Width);
        Assert.Equal(186, decoded.Height);
    }

    [Fact]
    public async Task BuildContactSheet_WithFakes_MatchesSnapshot()
    {
        var video = new Video("fake.mkv", new FakeCapturer(), new FakeProbe());

        var bytes = await video.BuildContactSheetAsync(TextFreeOptions());

        await Verify(bytes, "png");
    }

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
        BlankEvasion = false,
        Title = null,
    };
}
