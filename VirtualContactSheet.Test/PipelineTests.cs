namespace VirtualContactSheet.Test;

public class PipelineTests
{
    [Fact]
    public async Task BuildContactSheet_WithFakes_CapturesGridAndRenders()
    {
        var capturer = new FakeCapturer();
        var video = new Video("fake.mkv", capturer, new FakeProbe());

        var bytes = await video.BuildContactSheetAsync(TextFreeOptions(), ct: TestContext.Current.CancellationToken);
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

        var bytes = await video.BuildContactSheetAsync(TextFreeOptions(), ct: TestContext.Current.CancellationToken);

        await Verify(bytes, "png");
    }

    [Fact]
    public async Task BuildContactSheet_FrameCaptureFails_DrawsPlaceholderInItsCell()
    {
        // 1 column x 2 rows samples at 25s and 75s; the 25s frame is "corrupt".
        var capturer = new FailingCapturer(failAt: [25]);
        var video = new Video("fake.mkv", capturer, new FakeProbe());

        var bytes = await video.BuildContactSheetAsync(SingleColumnOptions(), ct: TestContext.Current.CancellationToken);
        using var decoded = SKBitmap.Decode(bytes);

        // The first frame fails and ffmpeg picks the height, so the size comes from the later frame.
        // Both rows are kept: 2 * (90 + 2) + 2, and the first cell (at margin 2) is the placeholder.
        Assert.Equal(186, decoded.Height);
        Assert.Equal(MissingFrame.Background, decoded.GetPixel(4, 4));
        Assert.Equal(MissingFrame.IconColor, decoded.GetPixel(2 + 80, 2 + 45));
    }

    [Fact]
    public async Task BuildContactSheet_EveryFrameCaptureFails_Throws()
    {
        var capturer = new FailingCapturer(failAt: [25, 75]);
        var video = new Video("fake.mkv", capturer, new FakeProbe());

        await Assert.ThrowsAsync<CaptureException>(
            () => video.BuildContactSheetAsync(SingleColumnOptions(), ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CaptureFrame_BlankEvasionCandidateFails_KeepsOriginalFrame()
    {
        var capturer = new FailingCapturer(failAt: [45], blankAt: [50]);
        var video = new Video("fake.mkv", capturer, new FakeProbe());

        using var frame = await video.CaptureFrameAsync(
            new TimeIndex(50), 160, 90, evadeBlank: true, alternatives: [-5], ct: TestContext.Current.CancellationToken);

        Assert.Equal(SKColors.Black, frame.GetPixel(0, 0));
    }

    private static ContactSheetOptions SingleColumnOptions()
    {
        var options = TextFreeOptions();
        options.Columns = 1;
        return options;
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
