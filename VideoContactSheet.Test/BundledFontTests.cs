namespace VideoContactSheet.Test;

public class BundledFontTests
{
    [Fact]
    public void DejaVuSans_RegularAndBold_AreEmbeddedInTheLibrary()
    {
        var resources = typeof(ContactSheet).Assembly.GetManifestResourceNames();

        Assert.Contains("VideoContactSheet.Fonts.DejaVuSans.ttf", resources);
        Assert.Contains("VideoContactSheet.Fonts.DejaVuSans-Bold.ttf", resources);
    }
}
