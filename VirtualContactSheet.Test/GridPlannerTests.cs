namespace VirtualContactSheet.Test;

public class GridPlannerTests
{
    private static readonly LayoutSpacing Plain = new(Gap: 2, Margin: 2, FrameSide: 0, FrameTop: 0, FrameBottom: 0);

    [Fact]
    public void Plan_FillsRowsLeftToRight()
    {
        var body = GridPlanner.Plan(count: 3, columns: 2, new SKSizeI(160, 90), Plain);

        Assert.Equal(
            [SKRectI.Create(2, 2, 160, 90), SKRectI.Create(164, 2, 160, 90), SKRectI.Create(2, 94, 160, 90)],
            body.Pictures);
    }

    [Fact]
    public void Plan_SizeCoversEveryColumnAndRowWithMargins()
    {
        var body = GridPlanner.Plan(count: 3, columns: 2, new SKSizeI(160, 90), Plain);

        // width = 2*160 + 3*2 ; height = 2 rows of (90 + 2) + 2
        Assert.Equal((326, 186), (body.Width, body.Height));
    }

    [Fact]
    public void Plan_LeavesRoomForTheFrameAroundEachPicture()
    {
        var framed = Plain with { FrameSide = 12, FrameTop = 12, FrameBottom = 36 };

        var body = GridPlanner.Plan(count: 2, columns: 2, new SKSizeI(160, 90), framed);

        // Cells are 184 x 138; the picture sits 12px in from the cell's left and top.
        Assert.Equal([SKRectI.Create(14, 14, 160, 90), SKRectI.Create(200, 14, 160, 90)], body.Pictures);
        Assert.Equal((2 + 184 + 2 + 184 + 2, 2 + 138 + 2), (body.Width, body.Height));
    }

    [Fact]
    public void For_DerivesSpacingFromTheOptions()
    {
        var options = new ContactSheetOptions { Padding = 5, SoftShadow = true, ShadowSize = 3, Polaroid = true };

        var spacing = LayoutSpacing.For(options);

        Assert.Equal(new LayoutSpacing(Gap: 5, Margin: 8, FrameSide: 12, FrameTop: 12, FrameBottom: 36), spacing);
    }
}
