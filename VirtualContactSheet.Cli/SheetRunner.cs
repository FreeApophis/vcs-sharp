using VirtualContactSheet.VideoProcessing;

namespace VirtualContactSheet.Cli;

/// <summary>Drives the actual work: load config, resolve the inputs, build options, then produce each sheet.</summary>
internal static class SheetRunner
{
    public static async Task<int> RunAsync(CliSettings settings, CancellationToken ct)
    {
        VcsConfig config;
        try
        {
            config = ConfigLoader.Load(settings.ConfigPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Config error: {ex.Message}");
            return 1;
        }

        // Everything that can be rejected before any work starts; the first error wins.
        var plan =
            from resolved in InputResolver.Resolve(settings.Files)
            from inputs in OptionScopeValidator.Validate(settings, resolved)
            from options in ContactSheetOptionsFactory.Create(settings, config)
            from imageOptions in ImageOptionsFactory.Create(settings, config)
            select (Inputs: inputs, Options: options, ImageOptions: imageOptions);

        return await plan.Match(
            left: error => Task.FromResult(Fail(error)),
            right: run => RunSheetsAsync(settings, run.Inputs, run.Options, run.ImageOptions, ct));
    }

    private static int Fail(string error)
    {
        Console.Error.WriteLine(error);
        return 1;
    }

    private static async Task<int> RunSheetsAsync(
        CliSettings settings,
        IReadOnlyList<SheetInput> inputs,
        ContactSheetOptions options,
        ImageOptions imageOptions,
        CancellationToken ct)
    {
        int exit = 0;
        for (int idx = 0; idx < inputs.Count; idx++)
        {
            var input = inputs[idx];
            var output = idx < settings.Outputs.Length
                ? settings.Outputs[idx]
                : $"{input.OutputBase}.{Formats.ExtensionFor(options.Format)}";

            try
            {
                await ProcessAsync(input, output, options, imageOptions, settings, ct);
            }
            catch (OperationCanceledException)
            {
                return 130;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed for '{input.DisplayName}': {ex.Message}");
                exit = 1;
                if (!settings.ContinueOnError)
                {
                    return exit;
                }
            }
        }

        return exit;
    }

    private static Task ProcessAsync(
        SheetInput input,
        string output,
        ContactSheetOptions options,
        ImageOptions imageOptions,
        CliSettings settings,
        CancellationToken ct)
    {
        if (!settings.Quiet)
        {
            Console.WriteLine($"Processing: {input.DisplayName} -> {output}");
        }

        return input switch
        {
            VideoInput video => ProcessVideoAsync(video, output, options, settings, ct),
            ImageInput images => ProcessImagesAsync(images, output, options, imageOptions, settings, ct),
        };
    }

    private static async Task ProcessVideoAsync(
        VideoInput input,
        string output,
        ContactSheetOptions options,
        CliSettings settings,
        CancellationToken ct)
    {
        var video = new Video(input.Path, ffBinaryFolder: settings.FfmpegFolder);
        if (!await video.IsValidAsync(ct))
        {
            throw new CaptureException("Not a valid video or no video stream.");
        }

        await video.SaveContactSheetAsync(output, options, Progress(settings), ct);
        Done(output, settings);
    }

    private static async Task ProcessImagesAsync(
        ImageInput input,
        string output,
        ContactSheetOptions options,
        ImageOptions imageOptions,
        CliSettings settings,
        CancellationToken ct)
    {
        var collection = ImageOptionsFactory.CreateCollection(input, imageOptions);
        if (collection.Count == 0)
        {
            throw new CaptureException(input switch
            {
                ImageFolderInput folder => $"No supported images found in '{folder.Folder}'.",
                LooseImagesInput => "No supported images given.",
            });
        }

        await collection.SaveContactSheetAsync(output, options, Progress(settings), ct);
        Done(output, settings);
    }

    private static IProgress<double>? Progress(CliSettings settings)
        => settings.Quiet ? null : new Progress<double>(p => Console.Write($"\r  {p:P0} captured   "));

    private static void Done(string output, CliSettings settings)
    {
        if (!settings.Quiet)
        {
            Console.WriteLine($"\r  Done: {output}            ");
        }
    }
}
