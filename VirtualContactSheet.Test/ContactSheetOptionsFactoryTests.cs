using VirtualContactSheet.Cli;

namespace VirtualContactSheet.Test;

public class ContactSheetOptionsFactoryTests
{
    [Fact]
    public void Defaults_PreserveLibraryToggleDefaults()
    {
        var options = Assert.Right(ContactSheetOptionsFactory.Create(Settings()));

        Assert.True(options.Timestamp);
        Assert.False(options.Polaroid);
        Assert.True(options.SoftShadow);
    }

    [Fact]
    public void PositiveAndNegativeFlags_AreApplied()
    {
        var settings = Settings() with { UsePolaroid = true, NoTimestamp = true };

        var options = Assert.Right(ContactSheetOptionsFactory.Create(settings));

        Assert.True(options.Polaroid);
        Assert.False(options.Timestamp);
    }

    [Fact]
    public void NegativeFlag_WinsOverPositiveFlag()
    {
        var settings = Settings() with { UseShadow = true, NoShadow = true };

        var options = Assert.Right(ContactSheetOptionsFactory.Create(settings));

        Assert.False(options.SoftShadow);
    }

    [Fact]
    public void GridAndFormat_AreMapped()
    {
        var settings = Settings() with { Columns = 6, Rows = 5, Width = 200, Format = "jpg" };

        var options = Assert.Right(ContactSheetOptionsFactory.Create(settings));

        Assert.Equal(6, options.Columns);
        Assert.Equal(5, options.Rows);
        Assert.Equal(200, options.ThumbnailWidth);
        Assert.Equal(SheetFormat.Jpg, options.Format);
    }

    [Fact]
    public void ValidHighlights_AreParsedOntoOptions()
    {
        var settings = Settings() with { HighlightStrings = ["1:00", "90"] };

        var options = Assert.Right(ContactSheetOptionsFactory.Create(settings));

        Assert.Equal([60d, 90], options.Highlights.Select(h => h.TotalSeconds));
    }

    [Fact]
    public void InvalidHighlight_FailsWithError()
    {
        var settings = Settings() with { HighlightStrings = ["not-a-time"] };

        var error = Assert.Left(ContactSheetOptionsFactory.Create(settings));

        Assert.Contains("not-a-time", error);
    }

    [Theory]
    [InlineData("16/9", 16f / 9f)]
    [InlineData("1.5", 1.5f)]
    [InlineData(" 4/3", 4f / 3f)]
    public void AspectRatio_NumberOrFraction_IsParsed(string value, float expected)
    {
        var options = Assert.Right(ContactSheetOptionsFactory.Create(Settings() with { AspectRatio = value }));

        Assert.Equal(expected, options.AspectRatio, 0.0001f);
    }

    [Theory]
    [InlineData("wide")]
    [InlineData("16/0")]
    [InlineData("16/x")]
    [InlineData("0")]
    [InlineData("-1.5")]
    public void AspectRatio_Invalid_FailsWithError(string value)
    {
        var error = Assert.Left(ContactSheetOptionsFactory.Create(Settings() with { AspectRatio = value }));

        Assert.Equal($"Invalid aspect ratio '{value}'. Expected a number (1.778) or fraction (16/9).", error);
    }

    [Fact]
    public void InvalidHighlight_IsReportedBeforeAnInvalidAspectRatio()
    {
        var settings = Settings() with { HighlightStrings = ["later"], AspectRatio = "wide" };

        var error = Assert.Left(ContactSheetOptionsFactory.Create(settings));

        Assert.Equal("Invalid highlight time: 'later'", error);
    }

    [Fact]
    public void ConfigColorsAndInterval_AreApplied()
    {
        var config = new VcsConfig
        {
            Main = new MainConfig { Interval = "1m30" },
            Style = new StyleConfig
            {
                Header = new TextStyleConfig { Color = "#ff0000", Background = "navy" },
                Contact = new BackgroundConfig { Background = "#00ff00" },
                Highlight = new BackgroundConfig { Background = "yellow" },
            },
        };

        var options = Assert.Right(ContactSheetOptionsFactory.Create(Settings(), config));

        Assert.Equal(90, options.Interval?.TotalSeconds);
        Assert.Equal(SKColors.Red, options.HeaderStyle.Color);
        Assert.Equal(SKColors.Navy, options.HeaderStyle.Background);
        Assert.Equal(SKColors.Lime, options.SheetBackground);
        Assert.Equal(SKColors.Yellow, options.HighlightBackground);
    }

    [Fact]
    public void InvalidConfigColorsAndInterval_AreIgnored()
    {
        var defaults = new ContactSheetOptions();
        var config = new VcsConfig
        {
            Main = new MainConfig { Interval = "soon" },
            Style = new StyleConfig
            {
                Header = new TextStyleConfig { Color = "#nope", Background = "chartreuse-ish" },
                Contact = new BackgroundConfig { Background = "#12" },
            },
        };

        var options = Assert.Right(ContactSheetOptionsFactory.Create(Settings(), config));

        Assert.Null(options.Interval);
        Assert.Equal(defaults.HeaderStyle.Color, options.HeaderStyle.Color);
        Assert.Equal(defaults.HeaderStyle.Background, options.HeaderStyle.Background);
        Assert.Equal(defaults.SheetBackground, options.SheetBackground);
    }

    [Fact]
    public void NoHeader_HidesTheHeader()
    {
        var options = Assert.Right(ContactSheetOptionsFactory.Create(Settings() with { NoHeader = true }));

        Assert.False(options.ShowHeader);
    }

    [Fact]
    public void HeaderFlag_OverridesConfigThatHidesIt()
    {
        var config = new VcsConfig { Filter = new FilterConfig { Header = false } };

        var hidden = Assert.Right(ContactSheetOptionsFactory.Create(Settings(), config));
        var shown = Assert.Right(ContactSheetOptionsFactory.Create(Settings() with { UseHeader = true }, config));

        Assert.False(hidden.ShowHeader);
        Assert.True(shown.ShowHeader);
    }

    [Fact]
    public void Padding_OverridesConfig()
    {
        var config = new VcsConfig { Main = new MainConfig { Padding = 20 } };

        var fromConfig = Assert.Right(ContactSheetOptionsFactory.Create(Settings(), config));
        var fromCli = Assert.Right(ContactSheetOptionsFactory.Create(Settings() with { Padding = 0 }, config));

        Assert.Equal(20, fromConfig.Padding);
        Assert.Equal(0, fromCli.Padding);
    }

    [Fact]
    public void NegativePadding_FailsWithError()
    {
        var error = Assert.Left(ContactSheetOptionsFactory.Create(Settings() with { Padding = -1 }));

        Assert.Contains("--padding", error);
    }

    private static CliSettings Settings() => new()
    {
        Files = ["in.mkv"],
        Outputs = [],
        Format = "png",
        HighlightStrings = [],
        Columns = 4,
        Rows = 4,
        Width = 320,
    };
}
