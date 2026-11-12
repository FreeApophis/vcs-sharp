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
        using var placeholder = MissingFrame.Render(160, 90);

        Assert.Equal(MissingFrame.Background, placeholder.GetPixel(2, 2));
        Assert.Equal(MissingFrame.IconColor, placeholder.GetPixel(80, 45));
    }
}
