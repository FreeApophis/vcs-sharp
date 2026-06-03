using VideoContactSheet.Cli;

namespace VideoContactSheet.Test;

public class ContactSheetOptionsFactoryTests
{
    [Fact]
    public void Defaults_PreserveLibraryToggleDefaults()
    {
        var ok = ContactSheetOptionsFactory.TryCreate(Settings(), out var options, out _);

        Assert.True(ok);
        Assert.True(options.Timestamp);
        Assert.False(options.Polaroid);
        Assert.True(options.SoftShadow);
    }

    [Fact]
    public void PositiveAndNegativeFlags_AreApplied()
    {
        var settings = Settings() with { UsePolaroid = true, NoTimestamp = true };

        ContactSheetOptionsFactory.TryCreate(settings, out var options, out _);

        Assert.True(options.Polaroid);
        Assert.False(options.Timestamp);
    }

    [Fact]
    public void NegativeFlag_WinsOverPositiveFlag()
    {
        var settings = Settings() with { UseShadow = true, NoShadow = true };

        ContactSheetOptionsFactory.TryCreate(settings, out var options, out _);

        Assert.False(options.SoftShadow);
    }

    [Fact]
    public void GridAndFormat_AreMapped()
    {
        var settings = Settings() with { Columns = 6, Rows = 5, Width = 200, Format = "jpg" };

        ContactSheetOptionsFactory.TryCreate(settings, out var options, out _);

        Assert.Equal(6, options.Columns);
        Assert.Equal(5, options.Rows);
        Assert.Equal(200, options.ThumbnailWidth);
        Assert.Equal(SheetFormat.Jpg, options.Format);
    }

    [Fact]
    public void ValidHighlights_AreParsedOntoOptions()
    {
        var settings = Settings() with { HighlightStrings = ["1:00", "90"] };

        var ok = ContactSheetOptionsFactory.TryCreate(settings, out var options, out _);

        Assert.True(ok);
        Assert.Equal([60d, 90], options.Highlights.Select(h => h.TotalSeconds));
    }

    [Fact]
    public void InvalidHighlight_FailsWithError()
    {
        var settings = Settings() with { HighlightStrings = ["not-a-time"] };

        var ok = ContactSheetOptionsFactory.TryCreate(settings, out _, out var error);

        Assert.False(ok);
        Assert.Contains("not-a-time", error);
    }

    private static CliSettings Settings() => new()
    {
        Files = ["in.mkv"],
        Outputs = Array.Empty<string>(),
        Format = "png",
        HighlightStrings = Array.Empty<string>(),
        Columns = 4,
        Rows = 4,
        Width = 320,
    };
}
