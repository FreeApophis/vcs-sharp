namespace VirtualContactSheet.Test;

public class MissingFrameTests
{
    [Fact]
    public void Render_HasTheRequestedSize()
    {
        using var placeholder = MissingFrame.Render(160, 90);

        Assert.Equal((160, 90), (placeholder.Width, placeholder.Height));
    }

    [Fact]
    public void Render_IsGrayWithAWarningIconInTheMiddle()
    {
        // Large enough that the exclamation bar covers whole pixels instead of anti-aliased ones.
        using var placeholder = MissingFrame.Render(800, 450);

        Assert.Equal(MissingFrame.Background, placeholder.GetPixel(2, 2));
        Assert.Equal(MissingFrame.IconColor, placeholder.GetPixel(400, 225));
    }
}
