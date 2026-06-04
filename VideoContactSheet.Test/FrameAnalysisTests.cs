namespace VideoContactSheet.Test;

public class FrameAnalysisTests
{
    [Fact]
    public void AverageBrightness_BlackFrame_ReturnsZero()
    {
        using var bitmap = TestFrames.Solid(320, 180, new SKColor(0, 0, 0));

        Assert.Equal(0.0, FrameAnalysis.AverageBrightness(bitmap));
    }

    [Fact]
    public void AverageBrightness_WhiteFrame_ReturnsOne()
    {
        using var bitmap = TestFrames.Solid(320, 180, new SKColor(255, 255, 255));

        Assert.Equal(1.0, FrameAnalysis.AverageBrightness(bitmap), precision: 10);
    }

    [Fact]
    public void AverageBrightness_HalfGrey_IsApproximatelyHalf()
    {
        using var bitmap = TestFrames.Solid(320, 180, new SKColor(128, 128, 128));

        // 128/255 ≈ 0.502; luma weights don't matter for a grey frame.
        Assert.InRange(FrameAnalysis.AverageBrightness(bitmap), 0.49, 0.51);
    }

    [Fact]
    public void AverageBrightness_SamplesWholeFrame_NotJustTopLeft()
    {
        // Black everywhere except the bottom-right quadrant which is white.
        // If sampling were limited to the top-left corner the result would be ~0.
        using var bitmap = new SKBitmap(320, 180);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(0, 0, 0));
        canvas.DrawRect(160, 90, 160, 90, new SKPaint { Color = new SKColor(255, 255, 255) });

        // One quarter of sampled pixels are white → brightness should be roughly 0.25.
        Assert.InRange(FrameAnalysis.AverageBrightness(bitmap), 0.15, 0.35);
    }

    [Fact]
    public void AverageBrightness_PureRed_ReflectsRedLumaWeight()
    {
        using var bitmap = TestFrames.Solid(320, 180, new SKColor(255, 0, 0));

        // Red luma weight is 0.2126 (Rec.709).
        Assert.Equal(0.2126, FrameAnalysis.AverageBrightness(bitmap), precision: 3);
    }
}
