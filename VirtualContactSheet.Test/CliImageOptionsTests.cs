using VirtualContactSheet.Cli;

namespace VirtualContactSheet.Test;

public class CliImageOptionsTests
{
    [Fact]
    public void Defaults_MatchTheLibraryDefaults()
    {
        var options = Assert.Right(ImageOptionsFactory.Create(Settings(), new VcsConfig()));

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
        var options = Assert.Right(ImageOptionsFactory.Create(Settings() with { Fit = value }, new VcsConfig()));

        Assert.Equal(expected, options.Fit);
    }

    [Fact]
    public void Fit_Invalid_ReportsTheAcceptedValues()
    {
        var error = Assert.Left(ImageOptionsFactory.Create(Settings() with { Fit = "squash" }, new VcsConfig()));

        Assert.Contains("contain, cover, stretch", error);
    }

    [Fact]
    public void RecursiveAndAll_AreApplied()
    {
        var settings = Settings() with { Recursive = true, AllImages = true };

        var options = Assert.Right(ImageOptionsFactory.Create(settings, new VcsConfig()));

        Assert.True(options.Recursive);
        Assert.Equal(ImageSelection.All, options.Selection);
    }

    [Fact]
    public void Config_SuppliesDefaults()
    {
        var config = new VcsConfig { Image = new ImageConfig { Fit = "cover", Recursive = true, All = true } };

        var options = Assert.Right(ImageOptionsFactory.Create(Settings(), config));

        Assert.Equal(ImageFit.Cover, options.Fit);
        Assert.True(options.Recursive);
        Assert.Equal(ImageSelection.All, options.Selection);
    }

    [Fact]
    public void CommandLine_OverridesConfig()
    {
        var config = new VcsConfig { Image = new ImageConfig { Fit = "cover" } };

        var options = Assert.Right(ImageOptionsFactory.Create(Settings() with { Fit = "stretch" }, config));

        Assert.Equal(ImageFit.Stretch, options.Fit);
    }

    [Fact]
    public void Config_InvalidFit_NamesTheConfigAsTheSource()
    {
        var config = new VcsConfig { Image = new ImageConfig { Fit = "nonsense" } };

        var error = Assert.Left(ImageOptionsFactory.Create(Settings(), config));

        Assert.Contains("config", error);
    }

    [Fact]
    public void Layout_DefaultsToGrid()
    {
        var options = Assert.Right(ImageOptionsFactory.Create(Settings(), new VcsConfig()));

        Assert.IsType<GridLayout>(options.Layout);
    }

    [Fact]
    public void Layout_Square_CropsToFill()
    {
        var options = Assert.Right(ImageOptionsFactory.Create(Settings() with { Layout = "Square" }, new VcsConfig()));

        Assert.IsType<SquareLayout>(options.Layout);
        Assert.Equal(ImageFit.Cover, options.Fit);
    }

    [Fact]
    public void Layout_Invalid_ReportsTheAcceptedValues()
    {
        var error = Assert.Left(ImageOptionsFactory.Create(Settings() with { Layout = "hexagon" }, new VcsConfig()));

        Assert.Contains("grid, square, masonry", error);
    }

    [Theory]
    [InlineData("square", "--fit")]
    [InlineData("square", "--aspect")]
    [InlineData("masonry", "--fit")]
    [InlineData("masonry", "--aspect")]
    public void Layout_WithACellShapeOptionItFixes_IsRejected(string layout, string option)
    {
        var settings = option == "--fit"
            ? Settings() with { Layout = layout, Fit = "contain" }
            : Settings() with { Layout = layout, AspectRatio = "16/9" };

        var error = Assert.Left(ImageOptionsFactory.Create(settings, new VcsConfig()));

        Assert.Contains(option, error);
        Assert.Contains(layout, error);
    }

    [Fact]
    public void Layout_Masonry_UsesEveryImageAndCrops()
    {
        var options = Assert.Right(ImageOptionsFactory.Create(Settings() with { Layout = "masonry" }, new VcsConfig()));

        Assert.IsType<MasonryLayout>(options.Layout);
        Assert.Equal(ImageSelection.All, options.Selection);
        Assert.Equal(ImageFit.Cover, options.Fit);
    }

    [Fact]
    public void Layout_MasonryWithAllSwitchedOffInConfig_Samples()
    {
        var config = new VcsConfig { Image = new ImageConfig { All = false } };

        var options = Assert.Right(ImageOptionsFactory.Create(Settings() with { Layout = "masonry" }, config));

        Assert.Equal(ImageSelection.Sample, options.Selection);
    }

    [Fact]
    public void Layout_FromConfig_IsOverriddenByTheCommandLine()
    {
        var config = new VcsConfig { Image = new ImageConfig { Layout = "square", Fit = "contain" } };

        var fromConfig = Assert.Right(ImageOptionsFactory.Create(Settings(), config));
        var fromCli = Assert.Right(ImageOptionsFactory.Create(Settings() with { Layout = "grid" }, config));

        // The square layout wins over the config's fit: it only makes sense cropped.
        Assert.IsType<SquareLayout>(fromConfig.Layout);
        Assert.Equal(ImageFit.Cover, fromConfig.Fit);
        Assert.IsType<GridLayout>(fromCli.Layout);
        Assert.Equal(ImageFit.Contain, fromCli.Fit);
    }

    [Fact]
    public void Create_AppliesTheLayout()
    {
        using var folder = new TempImageFolder();
        folder.Add("a.png");

        var collection = ImageOptionsFactory.CreateCollection(ResolveImages(folder.Root), new ImageOptions { Layout = new SquareLayout() });

        Assert.IsType<SquareLayout>(collection.Layout);
    }

    [Fact]
    public void Masonry_FlowAndColumnEnds_AreApplied()
    {
        var settings = Settings() with { Layout = "masonry", Flow = "Columns", ColumnEnds = "ragged" };

        var options = Assert.Right(ImageOptionsFactory.Create(settings, new VcsConfig()));

        Assert.Equal(new MasonryLayout(MasonryFlow.Columns, ColumnEnds.Ragged), options.Layout);
    }

    [Fact]
    public void Masonry_ColumnsEndCroppedByDefault()
    {
        var options = Assert.Right(ImageOptionsFactory.Create(Settings() with { Layout = "masonry", Flow = "columns" }, new VcsConfig()));

        Assert.Equal(new MasonryLayout(MasonryFlow.Columns, ColumnEnds.Cropped), options.Layout);
    }

    [Fact]
    public void Masonry_FlowAndColumnEndsFromConfig_AreApplied()
    {
        var config = new VcsConfig { Image = new ImageConfig { Layout = "masonry", Flow = "columns", ColumnEnds = "ragged" } };

        var options = Assert.Right(ImageOptionsFactory.Create(Settings(), config));

        Assert.Equal(new MasonryLayout(MasonryFlow.Columns, ColumnEnds.Ragged), options.Layout);
    }

    [Fact]
    public void Flow_InConfigWithoutMasonry_IsIgnored()
    {
        var config = new VcsConfig { Image = new ImageConfig { Flow = "columns" } };

        var options = Assert.Right(ImageOptionsFactory.Create(Settings(), config));

        Assert.IsType<GridLayout>(options.Layout);
    }

    [Theory]
    [InlineData("grid", "columns", null, 0, "--flow")]
    [InlineData("masonry", "rows", "ragged", 0, "--column-ends")]
    [InlineData("masonry", null, "ragged", 0, "--column-ends")]
    [InlineData("masonry", "columns", null, 200, "--height")]
    public void MasonryOption_ThatDoesNotApply_IsRejected(string layout, string? flow, string? ends, int height, string option)
    {
        var settings = Settings() with { Layout = layout, Flow = flow, ColumnEnds = ends, Height = height };

        var error = Assert.Left(ImageOptionsFactory.Create(settings, new VcsConfig()));

        Assert.StartsWith(option, error);
    }

    [Theory]
    [InlineData("diagonal", null, "rows, columns")]
    [InlineData(null, "jagged", "cropped, ragged")]
    public void Masonry_InvalidFlowOrColumnEnds_ReportsTheAcceptedValues(string? flow, string? ends, string accepted)
    {
        var settings = Settings() with { Layout = "masonry", Flow = flow ?? "columns", ColumnEnds = ends };

        var error = Assert.Left(ImageOptionsFactory.Create(settings, new VcsConfig()));

        Assert.Contains(accepted, error);
    }

    [Fact]
    public void Create_FromFolder_HonoursRecursion()
    {
        using var folder = new TempImageFolder();
        folder.Add("a.png");
        folder.Add(Path.Combine("nested", "b.png"));

        var input = ResolveImages(folder.Root);

        var flat = ImageOptionsFactory.CreateCollection(input, new ImageOptions());
        var deep = ImageOptionsFactory.CreateCollection(input, new ImageOptions { Recursive = true });

        Assert.Equal(1, flat.Count);
        Assert.Equal(2, deep.Count);
    }

    [Fact]
    public void Create_FromLooseFiles_KeepsTheGivenOrderAndHasNoFolderSource()
    {
        using var folder = new TempImageFolder();
        var second = folder.Add("z.png");
        var first = folder.Add("a.png");

        var input = ResolveImages(second, first);
        var collection = ImageOptionsFactory.CreateCollection(input, new ImageOptions());

        Assert.Equal([second, first], collection.Paths);
        Assert.Null(collection.Source);
    }

    [Fact]
    public void Create_AppliesTheSelectionMode()
    {
        using var folder = new TempImageFolder();
        folder.Add("a.png");

        var input = ResolveImages(folder.Root);

        var collection = ImageOptionsFactory.CreateCollection(input, new ImageOptions { Selection = ImageSelection.All });

        Assert.Equal(ImageSelection.All, collection.Selection);
    }

    private static ImageInput ResolveImages(params string[] arguments)
        => Assert.IsAssignableFrom<ImageInput>(Assert.Single(Assert.Right(InputResolver.Resolve(arguments))));

    private static CliSettings Settings() => new()
    {
        Files = ["photos"],
        Outputs = [],
        HighlightStrings = [],
    };
}
