namespace VirtualContactSheet.Test;

public class BundledFontTests
{
    [Fact]
    public void DejaVuSans_RegularAndBold_AreEmbeddedInTheLibrary()
    {
        var resources = typeof(ContactSheet).Assembly.GetManifestResourceNames();

        Assert.Contains("VirtualContactSheet.Fonts.DejaVuSans.ttf", resources);
        Assert.Contains("VirtualContactSheet.Fonts.DejaVuSans-Bold.ttf", resources);
    }
}
