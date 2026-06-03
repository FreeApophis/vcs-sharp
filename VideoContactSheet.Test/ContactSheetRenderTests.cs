namespace VideoContactSheet.Test;

public class ContactSheetRenderTests
{
    [Fact]
    public void Render_TextFreeSheet_HasExpectedDimensionsAndPixels()
    {
        var options = TextFreeOptions();
        var thumbnails = TestFrames.Grid(8); // eight 160x90 frames over 4 columns

        try
        {
            var bytes = new ContactSheet(options).Render(thumbnails);
            using var decoded = SKBitmap.Decode(bytes);

            // width = 4*160 + 5*2 ; height = 2 rows of (90 + 2) + 2
            Assert.Equal(650, decoded.Width);
            Assert.Equal(186, decoded.Height);

            // Top-left gutter is the sheet background; first thumbnail centre is palette[0].
            Assert.Equal(options.SheetBackground, decoded.GetPixel(0, 0));
            Assert.Equal(TestFrames.Palette[0], decoded.GetPixel(82, 47));
        }
        finally
        {
            TestFrames.DisposeAll(thumbnails);
        }
    }

    [Fact]
    public async Task Render_TextFreeSheet_MatchesSnapshot()
    {
        var thumbnails = TestFrames.Grid(8);

        try
        {
            var bytes = new ContactSheet(TextFreeOptions()).Render(thumbnails);
            await Verify(bytes, "png");
        }
        finally
        {
            TestFrames.DisposeAll(thumbnails);
        }
    }

    [Fact]
    public void Render_WithSignature_FooterBandReachesBottomEdge()
    {
        var options = TextFreeOptions();
        options.ShowSignature = true; // footer slate band must reach the ceil-rounded bottom row
        var thumbnails = TestFrames.Grid(8);

        try
        {
            var bytes = new ContactSheet(options).Render(thumbnails);
            using var decoded = SKBitmap.Decode(bytes);

            // Bottom-left carries no text, so it must be the footer background, not a white sliver.
            Assert.NotEqual(options.SheetBackground, options.SignatureStyle.Background);
            Assert.Equal(options.SignatureStyle.Background, decoded.GetPixel(0, decoded.Height - 1));
        }
        finally
        {
            TestFrames.DisposeAll(thumbnails);
        }
    }

    // Text-free so the snapshot is portable: no fonts (the one cross-machine variable) participate.
    private static ContactSheetOptions TextFreeOptions() => new()
    {
        Columns = 4,
        Padding = 2,
        SoftShadow = false,
        Polaroid = false,
        Timestamp = false,
        ShowHeader = false,
        ShowSignature = false,
        Title = null,
    };
}
