namespace VideoContactSheet.Cli;

/// <summary>Drives the actual work: build options from settings, then process each input file.</summary>
internal static class SheetRunner
{
    public static async Task<int> RunAsync(CliSettings settings, CancellationToken ct)
    {
        if (!ContactSheetOptionsFactory.TryCreate(settings, out var options, out var error))
        {
            Console.Error.WriteLine(error);
            return 1;
        }

        int exit = 0;
        for (int idx = 0; idx < settings.Files.Length; idx++)
        {
            var input = settings.Files[idx];
            var output = idx < settings.Outputs.Length
                ? settings.Outputs[idx]
                : Path.ChangeExtension(input, Formats.ExtensionFor(options.Format));

            try
            {
                await ProcessFileAsync(input, output, options, settings, ct);
            }
            catch (OperationCanceledException)
            {
                return 130;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed for '{input}': {ex.Message}");
                exit = 1;
                if (!settings.ContinueOnError)
                {
                    return exit;
                }
            }
        }

        return exit;
    }

    private static async Task ProcessFileAsync(string input, string output, ContactSheetOptions options, CliSettings settings, CancellationToken ct)
    {
        if (!settings.Quiet)
        {
            Console.WriteLine($"Processing: {input} -> {output}");
        }

        var video = new Video(input, ffBinaryFolder: settings.FfmpegFolder);
        if (!await video.IsValidAsync(ct))
        {
            throw new CaptureException("Not a valid video or no video stream.");
        }

        IProgress<double>? progress = settings.Quiet ? null
            : new Progress<double>(p => Console.Write($"\r  {p:P0} captured   "));

        await video.SaveContactSheetAsync(output, options, progress, ct);
        if (!settings.Quiet)
        {
            Console.WriteLine($"\r  Done: {output}            ");
        }
    }
}
