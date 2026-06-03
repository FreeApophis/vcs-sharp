namespace VideoContactSheet.Test;

public class HeaderAlignmentTests
{
    // Right-aligned header lines must end at the same x. This renders text, but the assertion is
    // font/OS-independent: two right-aligned lines ending in the same glyph ('8') share the same
    // right ink edge only when text width is measured correctly. The earlier char-as-glyph-id
    // measurement bug produced different edges here.
    [Fact]
    public void RightAlignedHeaderLines_OfDifferentLength_ShareTheSameRightEdge()
    {
        int shortEdge = RightInkEdge("8");
        int longEdge = RightInkEdge("888888");

        Assert.InRange(Math.Abs(shortEdge - longEdge), 0, 1);
    }

    /// <summary>Renders a header whose only content is one right-aligned line and returns the
    /// rightmost x carrying black text ink.</summary>
    private static int RightInkEdge(string rightLine)
    {
        var options = new ContactSheetOptions
        {
            Columns = 1,
            Rows = 1,
            Padding = 2,
            Timestamp = false,
            SoftShadow = false,
            Polaroid = false,
            ShowSignature = false,
            Title = null,
        };
        var thumbnails = TestFrames.Grid(1, 200, 60);

        try
        {
            var sheet = new ContactSheet(options)
            {
                HeaderOverride = new HeaderColumns(Array.Empty<string>(), [rightLine]),
            };
            var bytes = sheet.Render(thumbnails);
            using var image = SKBitmap.Decode(bytes);

            int maxX = -1;
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = image.Width - 1; x > maxX; x--)
                {
                    var p = image.GetPixel(x, y);
                    if (p.Red < 100 && p.Green < 100 && p.Blue < 100)
                    {
                        maxX = x;
                        break;
                    }
                }
            }

            return maxX;
        }
        finally
        {
            TestFrames.DisposeAll(thumbnails);
        }
    }
}
