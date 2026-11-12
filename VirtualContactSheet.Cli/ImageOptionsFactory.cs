using VirtualContactSheet.ImageProcessing;

namespace VirtualContactSheet.Cli;

/// <summary>Resolves the image-only knobs and builds the collection for one input.</summary>
internal static class ImageOptionsFactory
{
    /// <summary>Values accepted by <c>--fit</c>.</summary>
    public static readonly string[] AcceptedFits = ["contain", "cover", "stretch"];

    /// <summary>The image options, or the error for an invalid fit (the config's is checked first).</summary>
    public static Either<string, ImageOptions> Create(CliSettings settings, VcsConfig config)
        => from configuredFit in ParseFitSetting(config.Image?.Fit, "config")
           from requestedFit in ParseFitSetting(settings.Fit, "--fit")
           select Build(settings, config.Image, requestedFit.OrElse(configuredFit));

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
        return collection;
    }

    /// <summary>
    /// Library defaults, then config, then CLI args (which always win). The flags can only switch
    /// a setting on, so a config value stands unless the flag is given.
    /// </summary>
    private static ImageOptions Build(CliSettings settings, ImageConfig? image, Option<ImageFit> fit)
    {
        var defaults = new ImageOptions();
        bool recursive = settings.Recursive || (image?.Recursive ?? defaults.Recursive);
        bool all = settings.AllImages || (image?.All ?? defaults.Selection == ImageSelection.All);

        return defaults with
        {
            Fit = fit.GetOrElse(defaults.Fit),
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

    private static string FitError(string value, string source)
        => $"Invalid fit '{value}' in {source}. Expected one of: {string.Join(", ", AcceptedFits)}.";
}
