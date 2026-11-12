using VirtualContactSheet.ImageProcessing;

namespace VirtualContactSheet.Cli;

/// <summary>Resolves the image-only knobs and builds the collection for one input.</summary>
internal static class ImageOptionsFactory
{
    /// <summary>Values accepted by <c>--fit</c>.</summary>
    public static readonly string[] AcceptedFits = ["contain", "cover", "stretch"];

    /// <summary>Values accepted by <c>--layout</c>.</summary>
    public static readonly string[] AcceptedLayouts = ["grid", "square"];

    /// <summary>
    /// The image options, or the error for an invalid fit or layout (the config's are checked
    /// first), or for a cell shape option the layout overrides.
    /// </summary>
    public static Either<string, ImageOptions> Create(CliSettings settings, VcsConfig config)
        => from configuredFit in ParseFitSetting(config.Image?.Fit, "config")
           from requestedFit in ParseFitSetting(settings.Fit, "--fit")
           from configuredLayout in ParseLayoutSetting(config.Image?.Layout, "config")
           from requestedLayout in ParseLayoutSetting(settings.Layout, "--layout")
           let layout = requestedLayout.OrElse(configuredLayout).GetOrElse(new ImageOptions().Layout)
           from checkedLayout in RejectOverriddenCellShape(layout, settings)
           select Build(settings, config.Image, requestedFit.OrElse(configuredFit), checkedLayout);

    /// <summary>Builds the collection for one resolved image input.</summary>
    public static ImageCollection CreateCollection(ImageInput input, ImageOptions options)
    {
        var loader = new SkiaImageLoader(options.Fit);

        var collection = input switch
        {
            ImageFolderInput folder => ImageCollection.FromFolder(folder.Folder, options.Recursive, loader),
            LooseImagesInput loose => new ImageCollection(loose.Paths, loader),
        };

        collection.Selection = options.Selection;
        collection.Layout = options.Layout;
        return collection;
    }

    /// <summary>
    /// Library defaults, then config, then CLI args (which always win). The flags can only switch
    /// a setting on, so a config value stands unless the flag is given.
    /// </summary>
    private static ImageOptions Build(CliSettings settings, ImageConfig? image, Option<ImageFit> fit, ImageLayout layout)
    {
        var defaults = new ImageOptions();
        bool recursive = settings.Recursive || (image?.Recursive ?? defaults.Recursive);
        bool all = settings.AllImages || (image?.All ?? defaults.Selection == ImageSelection.All);

        return defaults with
        {
            // Square cells only make sense cropped, so the layout wins over a configured fit.
            Fit = layout is SquareLayout ? ImageFit.Cover : fit.GetOrElse(defaults.Fit),
            Layout = layout,
            Recursive = recursive,
            Selection = all ? ImageSelection.All : ImageSelection.Sample,
        };
    }

    /// <summary>None when the fit was not given; an error when it was given but is invalid.</summary>
    private static Either<string, Option<ImageFit>> ParseFitSetting(string? value, string source)
        => Option.FromNullable(value).Traverse(text => ParseFit(text).ToEither(FitError(text, source)));

    private static Option<ImageFit> ParseFit(string value)
        => value.Trim().ToLowerInvariant() switch
        {
            "contain" => ImageFit.Contain,
            "cover" => ImageFit.Cover,
            "stretch" => ImageFit.Stretch,
            _ => Option<ImageFit>.None,
        };

    /// <summary>None when the layout was not given; an error when it was given but is invalid.</summary>
    private static Either<string, Option<ImageLayout>> ParseLayoutSetting(string? value, string source)
        => Option.FromNullable(value).Traverse(text => ParseLayout(text).ToEither(
            $"Invalid layout '{text}' in {source}. Expected one of: {string.Join(", ", AcceptedLayouts)}."));

    private static Option<ImageLayout> ParseLayout(string value)
        => value.Trim().ToLowerInvariant() switch
        {
            "grid" => new GridLayout(),
            "square" => new SquareLayout(),
            _ => Option<ImageLayout>.None,
        };

    /// <summary>The square layout fixes the cell shape, so asking for another one is a mistake.</summary>
    private static Either<string, ImageLayout> RejectOverriddenCellShape(ImageLayout layout, CliSettings settings)
        => (layout, settings) switch
        {
            (SquareLayout, { Fit: not null }) => Either<string, ImageLayout>.Left(
                "--fit cannot be combined with --layout square: square cells are always cropped to fill."),
            (SquareLayout, { AspectRatio: not null }) => Either<string, ImageLayout>.Left(
                "--aspect cannot be combined with --layout square: square cells are always 1:1."),
            _ => Either<string, ImageLayout>.Right(layout),
        };

    private static string FitError(string value, string source)
        => $"Invalid fit '{value}' in {source}. Expected one of: {string.Join(", ", AcceptedFits)}.";
}
