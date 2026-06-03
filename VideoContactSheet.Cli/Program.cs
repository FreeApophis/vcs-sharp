using System.CommandLine;

namespace VideoContactSheet.Cli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        // ── Positional argument ──────────────────────────────────────────────────────
        var filesArg = new Argument<string[]>("files")
        {
            Description = "One or more video files to process.",
            Arity = ArgumentArity.OneOrMore,
        };

        // ── Grid options ─────────────────────────────────────────────────────────────
        var columnsOpt = new Option<int>("--columns", "-c")
        {
            Description = "Grid columns.",
            DefaultValueFactory = _ => 4,
        };
        var rowsOpt = new Option<int>("--rows", "-r")
        {
            Description = "Grid rows.",
            DefaultValueFactory = _ => 4,
        };
        var widthOpt = new Option<int>("--width", "-W")
        {
            Description = "Thumbnail width in pixels.",
            DefaultValueFactory = _ => 320,
        };

        // ── Time options ─────────────────────────────────────────────────────────────
        var intervalOpt = TimeIndexOption("--interval", "-i", "Capture interval (e.g. 3m30, 90, 1:22).");
        var fromOpt = TimeIndexOption("--from", "Start time.");
        var toOpt = TimeIndexOption("--to", "-t", "End time.");

        // ── Output options ───────────────────────────────────────────────────────────
        var outputOpt = new Option<string[]>("--output", "-o")
        {
            Description = "Output file (repeatable, paired with inputs).",
            Arity = ArgumentArity.ZeroOrMore,
        };

        var formatOpt = new Option<string>("--format", "-f")
        {
            Description = "Output format: png, jpg, jpeg, webp.",
            DefaultValueFactory = _ => "png",
        };
        formatOpt.AcceptOnlyFromAmong("png", "jpg", "jpeg", "webp");

        // ── Text / metadata options ──────────────────────────────────────────────────
        var titleOpt = new Option<string?>("--title", "-T")
        {
            Description = "Sheet title.",
        };
        var signatureOpt = new Option<string?>("--signature", "-s")
        {
            Description = "Footer signature text.",
        };
        var noSignatureOpt = new Option<bool>("--no-signature")
        {
            Description = "Remove footer signature.",
        };

        // ── Highlight frames ─────────────────────────────────────────────────────────
        var highlightOpt = new Option<string[]>("--highlight", "-l")
        {
            Description = "Add a highlight frame at TIME (repeatable).",
            Arity = ArgumentArity.ZeroOrMore,
        };

        // ── Style toggles ────────────────────────────────────────────────────────────
        var timestampOpt = new Option<bool>("--timestamp") { Description = "Enable timestamp overlay (default: on)." };
        var noTimestampOpt = new Option<bool>("--no-timestamp") { Description = "Disable timestamp overlay." };
        var polaroidOpt = new Option<bool>("--polaroid") { Description = "Enable polaroid frame (default: off)." };
        var noPolaroidOpt = new Option<bool>("--no-polaroid") { Description = "Disable polaroid frame." };
        var shadowOpt = new Option<bool>("--shadow") { Description = "Enable drop shadow (default: on)." };
        var noShadowOpt = new Option<bool>("--no-shadow") { Description = "Disable drop shadow." };

        // ── Tool / runtime options ───────────────────────────────────────────────────
        var ffmpegFolderOpt = new Option<string?>("--ffmpeg-folder")
        {
            Description = "Folder containing ffmpeg/ffprobe binaries (default: bundled or PATH).",
        };
        var quietOpt = new Option<bool>("--quiet", "-q") { Description = "Only print errors." };
        var continueOpt = new Option<bool>("--continue") { Description = "Continue with next file on error." };

        // ── Root command ─────────────────────────────────────────────────────────────
        var rootCommand = new RootCommand("Video Contact Sheet — generates frame-grid images from video files.")
        {
            filesArg,
            columnsOpt, rowsOpt, widthOpt,
            intervalOpt, fromOpt, toOpt,
            outputOpt, formatOpt,
            titleOpt, signatureOpt, noSignatureOpt,
            highlightOpt,
            timestampOpt, noTimestampOpt,
            polaroidOpt, noPolaroidOpt,
            shadowOpt, noShadowOpt,
            ffmpegFolderOpt, quietOpt, continueOpt,
        };

        rootCommand.SetAction(async (parseResult, ct) =>
        {
            var files = parseResult.GetValue(filesArg)!;
            var outputs = parseResult.GetValue(outputOpt) ?? [];
            var interval = parseResult.GetValue(intervalOpt);
            var from = parseResult.GetValue(fromOpt);
            var to = parseResult.GetValue(toOpt);
            var columns = parseResult.GetValue(columnsOpt);
            var rows = parseResult.GetValue(rowsOpt);
            var width = parseResult.GetValue(widthOpt);
            var format = parseResult.GetValue(formatOpt)!;
            var title = parseResult.GetValue(titleOpt);
            var signature = parseResult.GetValue(signatureOpt);
            var noSignature = parseResult.GetValue(noSignatureOpt);
            var highlightStrs = parseResult.GetValue(highlightOpt) ?? [];
            var useTimestamp = parseResult.GetValue(timestampOpt);
            var noTimestamp = parseResult.GetValue(noTimestampOpt);
            var usePolaroid = parseResult.GetValue(polaroidOpt);
            var noPolaroid = parseResult.GetValue(noPolaroidOpt);
            var useShadow = parseResult.GetValue(shadowOpt);
            var noShadow = parseResult.GetValue(noShadowOpt);
            var ffmpegFolder = parseResult.GetValue(ffmpegFolderOpt) ?? DetectBundledBinaries();
            var quiet = parseResult.GetValue(quietOpt);
            var continueOnErr = parseResult.GetValue(continueOpt);

            // Parse highlight times.
            var highlights = new List<TimeIndex>();
            foreach (var h in highlightStrs)
            {
                if (!TimeIndex.TryParse(h, out var ti))
                {
                    Console.Error.WriteLine($"Invalid highlight time: '{h}'");
                    return 1;
                }

                highlights.Add(ti);
            }

            // Build ContactSheetOptions — start from defaults and apply only what was set.
            var options = new ContactSheetOptions
            {
                Columns = columns,
                Rows = rows,
                ThumbnailWidth = width,
                Format = ParseFormat(format),
            };
            if (interval.HasValue)
            {
                options.Interval = interval;
            }

            if (from.HasValue)
            {
                options.From = from;
            }

            if (to.HasValue)
            {
                options.To = to;
            }

            if (title is not null)
            {
                options.Title = title;
            }

            if (signature is not null)
            {
                options.Signature = signature;
            }

            if (noSignature)
            {
                options.ShowSignature = false;
            }

            if (noTimestamp)
            {
                options.Timestamp = false;
            }
            else if (useTimestamp)
            {
                options.Timestamp = true;
            }

            if (noPolaroid)
            {
                options.Polaroid = false;
            }
            else if (usePolaroid)
            {
                options.Polaroid = true;
            }

            if (noShadow)
            {
                options.SoftShadow = false;
            }
            else if (useShadow)
            {
                options.SoftShadow = true;
            }

            options.Highlights.AddRange(highlights);

            // Process files.
            int exit = 0;
            for (int idx = 0; idx < files.Length; idx++)
            {
                var input = files[idx];
                var output = idx < outputs.Length
                    ? outputs[idx]
                    : Path.ChangeExtension(input, ExtFor(options.Format));

                try
                {
                    if (!quiet)
                    {
                        Console.WriteLine($"Processing: {input} -> {output}");
                    }

                    var video = new Video(input, ffBinaryFolder: ffmpegFolder);
                    if (!await video.IsValidAsync(ct))
                    {
                        throw new CaptureException("Not a valid video or no video stream.");
                    }

                    IProgress<double>? progress = quiet ? null
                        : new Progress<double>(p => Console.Write($"\r  {p:P0} captured   "));

                    await video.SaveContactSheetAsync(output, options, progress, ct);
                    if (!quiet)
                    {
                        Console.WriteLine($"\r  Done: {output}            ");
                    }
                }
                catch (OperationCanceledException)
                {
                    return 130;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed for '{input}': {ex.Message}");
                    exit = 1;
                    if (!continueOnErr)
                    {
                        return exit;
                    }
                }
            }

            return exit;
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static Option<TimeIndex?> TimeIndexOption(string name, string description)
        => TimeIndexOption(name, null, description);

    private static Option<TimeIndex?> TimeIndexOption(string name, string? alias, string description)
    {
        var opt = alias is not null
            ? new Option<TimeIndex?>(name, alias) { Description = description }
            : new Option<TimeIndex?>(name) { Description = description };

        opt.CustomParser = result =>
        {
            var token = result.Tokens.SingleOrDefault()?.Value;
            if (token is null)
            {
                return null;
            }

            if (!TimeIndex.TryParse(token, out var t))
            {
                result.AddError($"Invalid time '{token}'. Expected formats: 90, 1:22, 3m30, 1h2m3s.");
                return null;
            }

            return t;
        };

        return opt;
    }

    private static string? DetectBundledBinaries()
    {
        var dir = AppContext.BaseDirectory;
        var name = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        return File.Exists(Path.Combine(dir, name)) ? dir : null;
    }

    private static SheetFormat ParseFormat(string s) => s.ToLowerInvariant() switch
    {
        "jpg" or "jpeg" => SheetFormat.Jpg,
        "webp" => SheetFormat.Webp,
        _ => SheetFormat.Png,
    };

    private static string ExtFor(SheetFormat f) => f switch
    {
        SheetFormat.Jpg => "jpg",
        SheetFormat.Webp => "webp",
        _ => "png",
    };
}
