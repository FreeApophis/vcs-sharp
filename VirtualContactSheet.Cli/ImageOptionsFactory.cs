using VirtualContactSheet.ImageProcessing;

namespace VirtualContactSheet.Cli;

/// <summary>Resolves the image-only knobs and builds the collection for one input.</summary>
internal static class ImageOptionsFactory
{
    /// <summary>Values accepted by <c>--fit</c>.</summary>
    public static readonly string[] AcceptedFits = ["contain", "cover", "stretch"];

    public static bool TryCreate(CliSettings settings, VcsConfig config, out ImageOptions options, out string? error)
    {
        var result = new ImageOptions();

        if (config.Image is { } image)
        {
            if (image.Fit is { } configuredFit)
            {
                if (!TryParseFit(configuredFit, out var fit))
                {
                    options = result;
                    error = FitError(configuredFit, "config");
                    return false;
                }

                result = result with { Fit = fit };
            }

            if (image.Recursive is { } recursive)
            {
                result = result with { Recursive = recursive };
            }

            if (image.All is { } all)
            {
                result = result with { Selection = all ? ImageSelection.All : ImageSelection.Sample };
            }
        }

        // CLI args override config — only when explicitly provided.
        if (settings.Fit is { } requestedFit)
        {
            if (!TryParseFit(requestedFit, out var fit))
            {
                options = result;
                error = FitError(requestedFit, "--fit");
                return false;
            }

            result = result with { Fit = fit };
        }

        if (settings.Recursive)
        {
            result = result with { Recursive = true };
        }

        if (settings.AllImages)
        {
            result = result with { Selection = ImageSelection.All };
        }

        options = result;
        error = null;
        return true;
    }

    /// <summary>Builds the collection for one resolved image input.</summary>
    public static ImageCollection Create(SheetInput input, ImageOptions options)
    {
        var loader = new SkiaImageLoader(options.Fit);

        var collection = input.Folder is { } folder
            ? ImageCollection.FromFolder(folder, options.Recursive, loader)
            : new ImageCollection(input.Paths, loader);

        collection.Selection = options.Selection;
        return collection;
    }

    private static bool TryParseFit(string value, out ImageFit fit)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "contain":
                fit = ImageFit.Contain;
                return true;
            case "cover":
                fit = ImageFit.Cover;
                return true;
            case "stretch":
                fit = ImageFit.Stretch;
                return true;
            default:
                fit = ImageFit.Contain;
                return false;
        }
    }

    private static string FitError(string value, string source)
        => $"Invalid fit '{value}' in {source}. Expected one of: {string.Join(", ", AcceptedFits)}.";
}
