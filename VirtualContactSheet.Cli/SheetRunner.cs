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

        if (!InputResolver.TryResolve(settings.Files, out var inputs, out var inputError))
        {
            Console.Error.WriteLine(inputError);
            return 1;
        }

        if (!OptionScopeValidator.Validate(settings, inputs, out var scopeError))
        {
            Console.Error.WriteLine(scopeError);
            return 1;
        }

        if (!ContactSheetOptionsFactory.TryCreate(settings, config, out var options, out var error)
            || !ImageOptionsFactory.TryCreate(settings, config, out var imageOptions, out error))
        {
            Console.Error.WriteLine(error);
            return 1;
        }

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

        return input.Kind switch
        {
            SheetInputKind.Images => ProcessImagesAsync(input, output, options, imageOptions, settings, ct),
            _ => ProcessVideoAsync(input, output, options, settings, ct),
        };
    }

    private static async Task ProcessVideoAsync(
        SheetInput input,
        string output,
        ContactSheetOptions options,
        CliSettings settings,
        CancellationToken ct)
    {
        var video = new Video(input.Paths[0], ffBinaryFolder: settings.FfmpegFolder);
        if (!await video.IsValidAsync(ct))
        {
            throw new CaptureException("Not a valid video or no video stream.");
        }

        await video.SaveContactSheetAsync(output, options, Progress(settings), ct);
        Done(output, settings);
    }

    private static async Task ProcessImagesAsync(
        SheetInput input,
        string output,
        ContactSheetOptions options,
        ImageOptions imageOptions,
        CliSettings settings,
        CancellationToken ct)
    {
        var collection = ImageOptionsFactory.Create(input, imageOptions);
        if (collection.Count == 0)
        {
            throw new CaptureException(input.Folder is { } folder
                ? $"No supported images found in '{folder}'."
                : "No supported images given.");
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
