using VirtualContactSheet.Cli;

namespace VirtualContactSheet.Test;

public class OptionScopeValidatorTests
{
    private static readonly SheetInput VideoInput =
        new VideoInput(Path: "movie.mkv", DisplayName: "movie.mkv", OutputBase: "movie");

    private static readonly SheetInput ImageInput =
        new ImageFolderInput(Folder: "photos", DisplayName: "photos", OutputBase: "photos");

    [Theory]
    [InlineData("--interval")]
    [InlineData("--from")]
    [InlineData("--to")]
    [InlineData("--highlight")]
    public void VideoOnlyOption_WithOnlyImageInputs_IsRejected(string option)
    {
        var error = Assert.Left(OptionScopeValidator.Validate(WithVideoOption(option), [ImageInput]));

        Assert.Equal($"{option} applies to video inputs only.", error);
    }

    [Theory]
    [InlineData("--fit")]
    [InlineData("--recursive")]
    [InlineData("--all")]
    [InlineData("--layout")]
    public void ImageOnlyOption_WithOnlyVideoInputs_IsRejected(string option)
    {
        var error = Assert.Left(OptionScopeValidator.Validate(WithImageOption(option), [VideoInput]));

        Assert.Equal($"{option} applies to image inputs only.", error);
    }

    [Fact]
    public void VideoOnlyOption_WithMixedInputs_IsAccepted()
    {
        // The option is meaningful for the video half of the run, so it is not an error.
        var inputs = Assert.Right(OptionScopeValidator.Validate(WithVideoOption("--interval"), [VideoInput, ImageInput]));

        Assert.Equal([VideoInput, ImageInput], inputs);
    }

    [Fact]
    public void ImageOnlyOption_WithMixedInputs_IsAccepted()
    {
        var inputs = Assert.Right(OptionScopeValidator.Validate(WithImageOption("--fit"), [VideoInput, ImageInput]));

        Assert.Equal([VideoInput, ImageInput], inputs);
    }

    [Fact]
    public void SharedOptions_AreNeverRejected()
    {
        var settings = Settings() with { Columns = 3, Title = "Holiday", NoShadow = true };

        Assert.Right(OptionScopeValidator.Validate(settings, [ImageInput]));
        Assert.Right(OptionScopeValidator.Validate(settings, [VideoInput]));
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
        "--layout" => Settings() with { Layout = "square" },
        _ => Settings() with { AllImages = true },
    };

    private static CliSettings Settings() => new()
    {
        Files = [],
        Outputs = [],
        HighlightStrings = [],
    };
}
