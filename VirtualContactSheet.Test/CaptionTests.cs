namespace VirtualContactSheet.Test;

public class CaptionTests
{
    private const int ThumbWidth = 200;
    private const int ThumbHeight = 60;
    private const int Padding = 2;

    // Image captions are file names, which can be far wider than a thumbnail. They must be
    // ellipsized instead of spilling out over the neighbouring cell.
    [Fact]
    public void LongCaption_StaysInsideTheThumbnail()
    {
        var box = CaptionBox(new string('W', 200) + ".jpg");

        Assert.True(box.MaxX > box.MinX, "expected a caption box to be drawn");
        Assert.InRange(box.MinX, Padding, ThumbWidth);
        Assert.InRange(box.MaxX, Padding, Padding + ThumbWidth);
    }

    [Fact]
    public void ShortCaption_ProducesANarrowerBoxThanALongOne()
    {
        var shortBox = CaptionBox("a.jpg");
        var longBox = CaptionBox(new string('W', 200) + ".jpg");

        Assert.True(longBox.MaxX - longBox.MinX > shortBox.MaxX - shortBox.MinX);
    }

    [Fact]
    public void NoCaption_DrawsNoBox()
    {
        var box = CaptionBox(null);

        Assert.True(box.MaxX < box.MinX, "expected no caption box for an uncaptioned thumbnail");
    }

    /// <summary>Renders a single captioned thumbnail and returns the horizontal extent of the
    /// (dark, semi-transparent) caption background drawn over the solid colour frame.</summary>
    private static (int MinX, int MaxX) CaptionBox(string? caption)
    {
        var options = new ContactSheetOptions
        {
            Columns = 1,
            Rows = 1,
            Padding = Padding,
            Timestamp = true,
            SoftShadow = false,
            Polaroid = false,
            ShowHeader = false,
            ShowSignature = false,
            Title = null,
        };

        var frame = TestFrames.Solid(ThumbWidth, ThumbHeight, TestFrames.Palette[0]);
        var thumbnails = new List<ContactSheet.Thumbnail> { new(frame, caption) };

        try
        {
            var bytes = new ContactSheet(options).Render(thumbnails);
            using var decoded = SKBitmap.Decode(bytes);

            // A row inside the caption band, scanned across the frame only (the outer margin is
            // sheet background); the untouched frame there is Palette[0].
            int scanY = decoded.Height - Padding - 8;
            int minX = int.MaxValue;
            int maxX = -1;
            for (int x = Padding; x < Padding + ThumbWidth; x++)
            {
                if (decoded.GetPixel(x, scanY) != TestFrames.Palette[0])
                {
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                }
            }

            return (minX, maxX);
        }
        finally
        {
            TestFrames.DisposeAll(thumbnails);
        }
    }
}
