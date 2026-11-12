using VirtualContactSheet.ImageProcessing;

namespace VirtualContactSheet.Cli;

/// <summary>Resolves the image-only knobs and builds the collection for one input.</summary>
internal static class ImageOptionsFactory
{
    private static readonly Choice<ImageFit> Fits = new(
        "fit",
        [("contain", ImageFit.Contain), ("cover", ImageFit.Cover), ("stretch", ImageFit.Stretch)]);

    private static readonly Choice<ImageLayout> Layouts = new(
        "layout",
        [("grid", new GridLayout()), ("square", new SquareLayout()), ("masonry", new MasonryLayout())]);

    private static readonly Choice<MasonryFlow> Flows = new(
        "flow",
        [("rows", MasonryFlow.Rows), ("columns", MasonryFlow.Columns)]);

    private static readonly Choice<ColumnEnds> Ends = new(
        "column ends",
        [("cropped", ColumnEnds.Cropped), ("ragged", ColumnEnds.Ragged)]);

    /// <summary>Values accepted by <c>--fit</c>.</summary>
    public static string[] AcceptedFits => Fits.Names;

    /// <summary>Values accepted by <c>--layout</c>.</summary>
    public static string[] AcceptedLayouts => Layouts.Names;

    /// <summary>Values accepted by <c>--flow</c>.</summary>
    public static string[] AcceptedFlows => Flows.Names;

    /// <summary>Values accepted by <c>--column-ends</c>.</summary>
    public static string[] AcceptedColumnEnds => Ends.Names;

    /// <summary>
    /// The image options, or the error for an invalid value (the config's are checked first), for
    /// a masonry option the layout has no use for, or for a cell shape option the layout overrides.
    /// </summary>
    public static Either<string, ImageOptions> Create(CliSettings settings, VcsConfig config)
        => from configuredFit in Fits.Parse(config.Image?.Fit, "config")
           from requestedFit in Fits.Parse(settings.Fit, "--fit")
           from configuredLayout in Layouts.Parse(config.Image?.Layout, "config")
           from requestedLayout in Layouts.Parse(settings.Layout, "--layout")
           from configuredFlow in Flows.Parse(config.Image?.Flow, "config")
           from requestedFlow in Flows.Parse(settings.Flow, "--flow")
           from configuredEnds in Ends.Parse(config.Image?.ColumnEnds, "config")
           from requestedEnds in Ends.Parse(settings.ColumnEnds, "--column-ends")
           let layout = WithMasonryOptions(
               requestedLayout.OrElse(configuredLayout).GetOrElse(new ImageOptions().Layout),
               requestedFlow.OrElse(configuredFlow),
               requestedEnds.OrElse(configuredEnds))
           from applicableLayout in RejectMisappliedMasonryOptions(layout, settings)
           from checkedLayout in RejectOverriddenCellShape(applicableLayout, settings)
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

        // Masonry has no fixed number of cells to sample for, so it takes every image unless the
        // config explicitly says otherwise.
        bool all = settings.AllImages || (image?.All ?? layout is MasonryLayout);

        return defaults with
        {
            // Square and masonry cells are filled, so the layout wins over a configured fit.
            Fit = layout is SquareLayout or MasonryLayout ? ImageFit.Cover : fit.GetOrElse(defaults.Fit),
            Layout = layout,
            Recursive = recursive,
            Selection = all ? ImageSelection.All : ImageSelection.Sample,
        };
    }

    /// <summary>The flow and column ends only mean something to masonry; other layouts ignore them.</summary>
    private static ImageLayout WithMasonryOptions(ImageLayout layout, Option<MasonryFlow> flow, Option<ColumnEnds> ends)
        => layout is MasonryLayout masonry
            ? masonry with { Flow = flow.GetOrElse(masonry.Flow), Ends = ends.GetOrElse(masonry.Ends) }
            : layout;

    /// <summary>
    /// A masonry option given on the command line that the layout cannot use is a mistake. In
    /// config they are only defaults, so there they are ignored instead.
    /// </summary>
    private static Either<string, ImageLayout> RejectMisappliedMasonryOptions(ImageLayout layout, CliSettings settings)
        => (layout, settings) switch
        {
            (not MasonryLayout, { Flow: not null }) => Either<string, ImageLayout>.Left(
                "--flow applies to --layout masonry only."),
            (not MasonryLayout { Flow: MasonryFlow.Columns }, { ColumnEnds: not null }) => Either<string, ImageLayout>.Left(
                "--column-ends applies to --flow columns only."),
            (MasonryLayout { Flow: MasonryFlow.Columns }, { Height: > 0 }) => Either<string, ImageLayout>.Left(
                "--height cannot be combined with --flow columns: the column width sets every image's height."),
            _ => Either<string, ImageLayout>.Right(layout),
        };

    /// <summary>Square and masonry fix the cell shape, so asking for another one is a mistake.</summary>
    private static Either<string, ImageLayout> RejectOverriddenCellShape(ImageLayout layout, CliSettings settings)
        => (layout, settings) switch
        {
            (SquareLayout, { Fit: not null }) => CellShapeError("--fit", "square", "square cells are always cropped to fill"),
            (SquareLayout, { AspectRatio: not null }) => CellShapeError("--aspect", "square", "square cells are always 1:1"),
            (MasonryLayout, { Fit: not null }) => CellShapeError("--fit", "masonry", "every image keeps its own proportions"),
            (MasonryLayout, { AspectRatio: not null }) => CellShapeError("--aspect", "masonry", "every image keeps its own proportions"),
            _ => Either<string, ImageLayout>.Right(layout),
        };

    private static Either<string, ImageLayout> CellShapeError(string option, string layout, string reason)
        => Either<string, ImageLayout>.Left($"{option} cannot be combined with --layout {layout}: {reason}.");

    /// <summary>A setting with a fixed set of named values, matched case-insensitively.</summary>
    private sealed record Choice<T>(string Kind, IReadOnlyList<(string Name, T Value)> Values)
        where T : notnull
    {
        public string[] Names => [.. Values.Select(value => value.Name)];

        /// <summary>None when the setting was not given; an error when it was given but is invalid.</summary>
        public Either<string, Option<T>> Parse(string? text, string source)
            => Option.FromNullable(text).Traverse(given => Values
                .FirstOrNone(value => value.Name == given.Trim().ToLowerInvariant())
                .Select(value => value.Value)
                .ToEither($"Invalid {Kind} '{given}' in {source}. Expected one of: {string.Join(", ", Names)}."));
    }
}
