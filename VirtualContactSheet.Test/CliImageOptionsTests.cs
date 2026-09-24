using VirtualContactSheet.Cli;

namespace VirtualContactSheet.Test;

public class CliImageOptionsTests
{
    [Fact]
    public void Defaults_MatchTheLibraryDefaults()
    {
        var ok = ImageOptionsFactory.TryCreate(Settings(), new VcsConfig(), out var options, out _);

        Assert.True(ok);
        Assert.Equal(ImageFit.Contain, options.Fit);
        Assert.False(options.Recursive);
        Assert.Equal(ImageSelection.Sample, options.Selection);
    }

    [Theory]
    [InlineData("contain", ImageFit.Contain)]
    [InlineData("cover", ImageFit.Cover)]
    [InlineData("stretch", ImageFit.Stretch)]
    [InlineData("COVER", ImageFit.Cover)]
    public void Fit_IsParsedCaseInsensitively(string value, ImageFit expected)
    {
        ImageOptionsFactory.TryCreate(Settings() with { Fit = value }, new VcsConfig(), out var options, out _);

        Assert.Equal(expected, options.Fit);
    }

    [Fact]
    public void Fit_Invalid_ReportsTheAcceptedValues()
    {
        var ok = ImageOptionsFactory.TryCreate(Settings() with { Fit = "squash" }, new VcsConfig(), out _, out var error);

        Assert.False(ok);
        Assert.Contains("contain, cover, stretch", error);
    }

    [Fact]
    public void RecursiveAndAll_AreApplied()
    {
        var settings = Settings() with { Recursive = true, AllImages = true };

        ImageOptionsFactory.TryCreate(settings, new VcsConfig(), out var options, out _);

        Assert.True(options.Recursive);
        Assert.Equal(ImageSelection.All, options.Selection);
    }

    [Fact]
    public void Config_SuppliesDefaults()
    {
        var config = new VcsConfig { Image = new ImageConfig { Fit = "cover", Recursive = true, All = true } };

        ImageOptionsFactory.TryCreate(Settings(), config, out var options, out _);

        Assert.Equal(ImageFit.Cover, options.Fit);
        Assert.True(options.Recursive);
        Assert.Equal(ImageSelection.All, options.Selection);
    }

    [Fact]
    public void CommandLine_OverridesConfig()
    {
        var config = new VcsConfig { Image = new ImageConfig { Fit = "cover" } };

        ImageOptionsFactory.TryCreate(Settings() with { Fit = "stretch" }, config, out var options, out _);

        Assert.Equal(ImageFit.Stretch, options.Fit);
    }

    [Fact]
    public void Config_InvalidFit_NamesTheConfigAsTheSource()
    {
        var config = new VcsConfig { Image = new ImageConfig { Fit = "nonsense" } };

        var ok = ImageOptionsFactory.TryCreate(Settings(), config, out _, out var error);

        Assert.False(ok);
        Assert.Contains("config", error);
    }

    [Fact]
    public void Create_FromFolder_HonoursRecursion()
    {
        using var folder = new TempImageFolder();
        folder.Add("a.png");
        folder.Add(Path.Combine("nested", "b.png"));

        var input = Assert.Single(ResolveOne(folder.Root));

        var flat = ImageOptionsFactory.Create(input, new ImageOptions());
        var deep = ImageOptionsFactory.Create(input, new ImageOptions { Recursive = true });

        Assert.Equal(1, flat.Count);
        Assert.Equal(2, deep.Count);
    }

    [Fact]
    public void Create_FromLooseFiles_KeepsTheGivenOrderAndHasNoFolderSource()
    {
        using var folder = new TempImageFolder();
        var second = folder.Add("z.png");
        var first = folder.Add("a.png");

        var input = Assert.Single(ResolveOne(second, first));
        var collection = ImageOptionsFactory.Create(input, new ImageOptions());

        Assert.Equal([second, first], collection.Paths);
        Assert.Null(collection.Source);
    }

    [Fact]
    public void Create_AppliesTheSelectionMode()
    {
        using var folder = new TempImageFolder();
        folder.Add("a.png");

        var input = Assert.Single(ResolveOne(folder.Root));

        var collection = ImageOptionsFactory.Create(input, new ImageOptions { Selection = ImageSelection.All });

        Assert.Equal(ImageSelection.All, collection.Selection);
    }

    private static IReadOnlyList<SheetInput> ResolveOne(params string[] arguments)
    {
        Assert.True(InputResolver.TryResolve(arguments, out var inputs, out var error), error);
        return inputs;
    }

    private static CliSettings Settings() => new()
    {
        Files = ["photos"],
        Outputs = [],
        HighlightStrings = [],
    };
}
