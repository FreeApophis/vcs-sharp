namespace VirtualContactSheet.Test;

public class MasonryPlannerTests
{
    private static readonly LayoutSpacing Tight = new(Gap: 0, Margin: 0, FrameSide: 0, FrameTop: 0, FrameBottom: 0);

    [Fact]
    public void Rows_AFullRowIsScaledToTheExactWidth()
    {
        var body = MasonryPlanner.Rows([1, 1, 2], width: 400, targetHeight: 100, Tight);

        Assert.Equal(
            [SKRectI.Create(0, 0, 100, 100), SKRectI.Create(100, 0, 100, 100), SKRectI.Create(200, 0, 200, 100)],
            body.Pictures);
        Assert.Equal((400, 100), (body.Width, body.Height));
    }

    [Fact]
    public void Rows_TheLastRowKeepsTheTargetHeightAndIsNotStretched()
    {
        var body = MasonryPlanner.Rows([1, 1, 2, 1, 1], width: 400, targetHeight: 100, Tight);

        Assert.Equal([SKRectI.Create(0, 100, 100, 100), SKRectI.Create(100, 100, 100, 100)], body.Pictures.Skip(3));
        Assert.Equal(200, body.Height);
    }

    [Fact]
    public void Rows_ARowClosesWhereItsHeightIsClosestToTheTarget()
    {
        // Three squares justify to 133px, four to 100px: 133 is closer to 120.
        var body = MasonryPlanner.Rows([1, 1, 1, 1, 1], width: 400, targetHeight: 120, Tight);

        var firstRow = body.Pictures.Where(p => p.Top == 0).ToList();
        Assert.Equal(3, firstRow.Count);
        Assert.All(firstRow, p => Assert.Equal(133, p.Height));
        Assert.Equal(400, firstRow[^1].Right);
    }

    [Fact]
    public void Rows_LeavesMarginsGapsAndFramesAroundThePictures()
    {
        var spacing = new LayoutSpacing(Gap: 2, Margin: 2, FrameSide: 1, FrameTop: 1, FrameBottom: 3);

        // Inner width 402 holds 4 squares: 402 - 4 * 2 (frames) - 3 * 2 (gaps) = 388 → 97px each.
        var body = MasonryPlanner.Rows([1, 1, 1, 1], width: 406, targetHeight: 100, spacing);

        Assert.Equal(
            [SKRectI.Create(3, 3, 97, 97), SKRectI.Create(104, 3, 97, 97), SKRectI.Create(205, 3, 97, 97), SKRectI.Create(306, 3, 97, 97)],
            body.Pictures);
        Assert.Equal(2 + 1 + 97 + 3 + 2, body.Height);
    }

    [Fact]
    public void Rows_EveryFullRowEndsAtTheRightEdge()
    {
        double[] aspects = [1.5, 0.75, 1, 2.2, 0.6, 1.33, 1.78, 0.8, 1, 1.5, 0.66, 2];

        var body = MasonryPlanner.Rows(aspects, width: 1000, targetHeight: 180, Tight);

        var rows = body.Pictures.GroupBy(p => p.Top).ToList();
        Assert.True(rows.Count > 1);
        Assert.All(rows.SkipLast(1), row => Assert.Equal(1000, row.Max(p => p.Right)));
        Assert.All(rows, row => Assert.Single(row.Select(p => p.Height).Distinct()));
    }

    [Fact]
    public void Rows_APanoramaWiderThanTheSheetIsScaledDownToFit()
    {
        var body = MasonryPlanner.Rows([10], width: 400, targetHeight: 100, Tight);

        Assert.Equal([SKRectI.Create(0, 0, 400, 40)], body.Pictures);
    }

    [Fact]
    public void Columns_EachImageGoesIntoTheShortestColumnLeftmostFirst()
    {
        var body = MasonryPlanner.Columns([1, 0.5, 1, 1], columns: 2, columnWidth: 100, ColumnEnds.Ragged, Tight);

        Assert.Equal(
            [SKRectI.Create(0, 0, 100, 100), SKRectI.Create(100, 0, 100, 200), SKRectI.Create(0, 100, 100, 100), SKRectI.Create(0, 200, 100, 100)],
            body.Pictures);
    }

    [Fact]
    public void Columns_RaggedIsAsTallAsTheTallestColumn()
    {
        var body = MasonryPlanner.Columns([1, 0.5, 1, 1], columns: 2, columnWidth: 100, ColumnEnds.Ragged, Tight);

        Assert.Equal((200, 300), (body.Width, body.Height));
    }

    [Fact]
    public void Columns_CroppedEndsEveryColumnAtTheMeanHeight()
    {
        // Columns of 300 and 200 meet at 250: the first squeezes three images, the second stretches one.
        var body = MasonryPlanner.Columns([1, 0.5, 1, 1], columns: 2, columnWidth: 100, ColumnEnds.Cropped, Tight);

        Assert.Equal(
            [SKRectI.Create(0, 0, 100, 83), SKRectI.Create(100, 0, 100, 250), SKRectI.Create(0, 83, 100, 84), SKRectI.Create(0, 167, 100, 83)],
            body.Pictures);
        Assert.Equal(250, body.Height);
    }

    [Fact]
    public void Columns_LeavesMarginsGapsAndFramesAroundThePictures()
    {
        var spacing = new LayoutSpacing(Gap: 2, Margin: 2, FrameSide: 1, FrameTop: 1, FrameBottom: 3);

        var body = MasonryPlanner.Columns([1, 1, 1], columns: 2, columnWidth: 100, ColumnEnds.Ragged, spacing);

        // Cells are 102 x 104; the third image goes below the first, one gap further down.
        Assert.Equal(
            [SKRectI.Create(3, 3, 100, 100), SKRectI.Create(107, 3, 100, 100), SKRectI.Create(3, 109, 100, 100)],
            body.Pictures);
        Assert.Equal((2 + 102 + 2 + 102 + 2, 2 + 104 + 2 + 104 + 2), (body.Width, body.Height));
    }

    [Fact]
    public void Columns_CroppedIgnoresEmptyColumnsAndKeepsTheirWidth()
    {
        var body = MasonryPlanner.Columns([1], columns: 3, columnWidth: 100, ColumnEnds.Cropped, Tight);

        Assert.Equal([SKRectI.Create(0, 0, 100, 100)], body.Pictures);
        Assert.Equal((300, 100), (body.Width, body.Height));
    }
}
