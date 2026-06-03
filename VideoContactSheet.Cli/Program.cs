using VideoContactSheet;

namespace VideoContactSheet.Cli;

internal static class Program
{
    private const string Version = "1.0.0";

    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            PrintHelp();
            return args.Length == 0 ? 1 : 0;
        }

        if (args.Contains("-v") || args.Contains("--version"))
        {
            Console.WriteLine($"Video Contact Sheet .NET {Version}");
            return 0;
        }

        var options = new ContactSheetOptions();
        var inputs = new List<string>();
        var outputs = new List<string>();
        string? ffBinaryFolder = null;
        bool quiet = false;
        bool keepGoing = false;

        try
        {
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                string Next() => ++i < args.Length ? args[i] : throw new ArgumentException($"Missing value for {a}");

                switch (a)
                {
                    case "-i" or "--interval": options.Interval = TimeIndex.Parse(Next()); break;
                    case "-c" or "--columns": options.Columns = int.Parse(Next()); break;
                    case "-r" or "--rows": options.Rows = int.Parse(Next()); break;
                    case "-W" or "--width": options.ThumbnailWidth = int.Parse(Next()); break;
                    case "--from": options.From = TimeIndex.Parse(Next()); break;
                    case "-t" or "--to": options.To = TimeIndex.Parse(Next()); break;
                    case "-f" or "--format": options.Format = ParseFormat(Next()); break;
                    case "-T" or "--title": options.Title = Next(); break;
                    case "-o" or "--output": outputs.Add(Next()); break;
                    case "-s" or "--signature": options.Signature = Next(); break;
                    case "--no-signature": options.ShowSignature = false; break;
                    case "-l" or "--highlight": options.Highlights.Add(TimeIndex.Parse(Next())); break;
                    case "--timestamp": options.Timestamp = true; break;
                    case "--no-timestamp": options.Timestamp = false; break;
                    case "--polaroid": options.Polaroid = true; break;
                    case "--no-polaroid": options.Polaroid = false; break;
                    case "--shadow": options.SoftShadow = true; break;
                    case "--no-shadow": options.SoftShadow = false; break;
                    case "--ffmpeg-folder": ffBinaryFolder = Next(); break;
                    case "-q" or "--quiet": quiet = true; break;
                    case "--continue": keepGoing = true; break;
                    default:
                        if (a.StartsWith('-')) throw new ArgumentException($"Unknown option: {a}");
                        inputs.Add(a);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error parsing arguments: {ex.Message}");
            return 1;
        }

        if (inputs.Count == 0)
        {
            Console.Error.WriteLine("No input file specified.");
            return 1;
        }

        // Auto-detect bundled binaries next to the executable (takes precedence over PATH).
        ffBinaryFolder ??= DetectBundledBinaries();

        int exit = 0;
        for (int idx = 0; idx < inputs.Count; idx++)
        {
            string input = inputs[idx];
            string output = idx < outputs.Count
                ? outputs[idx]
                : Path.ChangeExtension(input, ExtFor(options.Format));

            try
            {
                if (!quiet) Console.WriteLine($"Processing: {input} -> {output}");
                var video = new Video(input, ffBinaryFolder: ffBinaryFolder);
                if (!await video.IsValidAsync())
                    throw new CaptureException("Not a valid video or no video stream.");

                IProgress<double>? progress = quiet ? null
                    : new Progress<double>(p => Console.Write($"\r  {p:P0} captured   "));

                await video.SaveContactSheetAsync(output, options, progress);
                if (!quiet) Console.WriteLine($"\r  Done: {output}            ");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed for '{input}': {ex.Message}");
                exit = 1;
                if (!keepGoing) return exit;
            }
        }

        return exit;
    }

    private static string? DetectBundledBinaries()
    {
        var dir = AppContext.BaseDirectory;
        var name = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        return File.Exists(Path.Combine(dir, name)) ? dir : null;
    }

    private static SheetFormat ParseFormat(string s) => s.ToLowerInvariant() switch
    {
        "png" => SheetFormat.Png,
        "jpg" or "jpeg" => SheetFormat.Jpg,
        "webp" => SheetFormat.Webp,
        _ => throw new ArgumentException($"Unsupported format: {s}"),
    };

    private static string ExtFor(SheetFormat f) => f switch
    {
        SheetFormat.Jpg => "jpg",
        SheetFormat.Webp => "webp",
        _ => "png",
    };

    private static void PrintHelp()
    {
        Console.WriteLine($"""
            Video Contact Sheet .NET {Version}

            Usage: vcs [options] <video> [<video> ...]

                -i, --interval [INTERVAL]   Capture at this interval (e.g. 3m30, 90, 1:22)
                -c, --columns [COLUMNS]     Number of columns (default 4)
                -r, --rows [ROWS]           Number of rows (default 4)
                -W, --width [WIDTH]         Thumbnail width in px (default 320)
                    --from [FROM]           Start time
                -t, --to [TO]               End time
                -f, --format [FORMAT]       png, jpg, jpeg, webp
                -T, --title [TITLE]         Sheet title
                -o, --output [FILE]         Output file (repeatable, paired with inputs)
                -s, --signature [TEXT]      Footer signature text
                    --no-signature          Remove footer signature
                -l, --highlight [TIME]      Add a highlight frame at TIME (repeatable)
                    --[no-]timestamp        Timestamp overlay (default on)
                    --[no-]polaroid         Polaroid frame (default off)
                    --[no-]shadow           Drop shadow (default on)
                    --ffmpeg-folder [DIR]    Folder containing ffmpeg/ffprobe binaries
                -q, --quiet                 Only print errors
                    --continue              Continue with next file on error
                -v, --version               Print version
                -h, --help                  Show this help

            Examples:
              vcs video.avi
              vcs -i 3m30 input.wmv -o output.jpg
              vcs --from 3m --to 18m -i 2m input.avi
            """);
    }
}
