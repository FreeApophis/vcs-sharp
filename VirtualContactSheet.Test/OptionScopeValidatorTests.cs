using VirtualContactSheet.Cli;

namespace VirtualContactSheet.Test;

public class OptionScopeValidatorTests
{
    private static readonly SheetInput VideoInput = new()
    {
        Kind = SheetInputKind.Video,
        Paths = ["movie.mkv"],
        DisplayName = "movie.mkv",
        OutputBase = "movie",
    };

    private static readonly SheetInput ImageInput = new()
    {
        Kind = SheetInputKind.Images,
        Paths = ["photos"],
        Folder = "photos",
        DisplayName = "photos",
        OutputBase = "photos",
    };

    [Theory]
    [InlineData("--interval")]
    [InlineData("--from")]
    [InlineData("--to")]
    [InlineData("--highlight")]
    public void VideoOnlyOption_WithOnlyImageInputs_IsRejected(string option)
    {
        var ok = OptionScopeValidator.Validate(WithVideoOption(option), [ImageInput], out var error);

        Assert.False(ok);
        Assert.Equal($"{option} applies to video inputs only.", error);
    }

    [Theory]
    [InlineData("--fit")]
    [InlineData("--recursive")]
    [InlineData("--all")]
    public void ImageOnlyOption_WithOnlyVideoInputs_IsRejected(string option)
    {
        var ok = OptionScopeValidator.Validate(WithImageOption(option), [VideoInput], out var error);

        Assert.False(ok);
        Assert.Equal($"{option} applies to image inputs only.", error);
    }

    [Fact]
    public void VideoOnlyOption_WithMixedInputs_IsAccepted()
    {
        // The option is meaningful for the video half of the run, so it is not an error.
        var ok = OptionScopeValidator.Validate(WithVideoOption("--interval"), [VideoInput, ImageInput], out var error);

        Assert.True(ok);
        Assert.Null(error);
    }

    [Fact]
    public void ImageOnlyOption_WithMixedInputs_IsAccepted()
    {
        var ok = OptionScopeValidator.Validate(WithImageOption("--fit"), [VideoInput, ImageInput], out _);

        Assert.True(ok);
    }

    [Fact]
    public void SharedOptions_AreNeverRejected()
    {
        var settings = Settings() with { Columns = 3, Title = "Holiday", NoShadow = true };

        Assert.True(OptionScopeValidator.Validate(settings, [ImageInput], out _));
        Assert.True(OptionScopeValidator.Validate(settings, [VideoInput], out _));
    }

    private static CliSettings WithVideoOption(string option) => option switch
    {
        "--interval" => Settings() with { Interval = new TimeIndex(30) },
        "--from" => Settings() with { From = new TimeIndex(10) },
        "--to" => Settings() with { To = new TimeIndex(20) },
        _ => Settings() with { HighlightStrings = ["1:00"] },
    };

    private static CliSettings WithImageOption(string option) => option switch
    {
        "--fit" => Settings() with { Fit = "cover" },
        "--recursive" => Settings() with { Recursive = true },
        _ => Settings() with { AllImages = true },
    };

    private static CliSettings Settings() => new()
    {
        Files = [],
        Outputs = [],
        HighlightStrings = [],
    };
}
