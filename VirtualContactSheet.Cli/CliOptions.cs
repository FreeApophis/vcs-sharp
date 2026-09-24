using System.CommandLine;

namespace VirtualContactSheet.Cli;

/// <summary>
/// Owns every <see cref="Argument"/>/<see cref="Option"/> the CLI exposes, assembles them into
/// a <see cref="RootCommand"/>, and binds a parsed result back into a <see cref="CliSettings"/>.
/// </summary>
internal sealed class CliOptions
{
    // ── Positional argument ──────────────────────────────────────────────────────
    private readonly Argument<string[]> _files = new("files")
    {
        Description = "Video files, image files, or folders of images. "
                    + "Each video and each folder produces its own sheet; loose image files are combined into one.",
        Arity = ArgumentArity.OneOrMore,
    };

    // ── Grid options ─────────────────────────────────────────────────────────────
    private readonly Option<int> _columns = new("--columns", "-c")
    {
        Description = "Grid columns.",
        DefaultValueFactory = _ => 4,
    };

    private readonly Option<int> _rows = new("--rows", "-r")
    {
        Description = "Grid rows.",
        DefaultValueFactory = _ => 4,
    };

    private readonly Option<int> _width = new("--width", "-W")
    {
        Description = "Thumbnail width in pixels.",
        DefaultValueFactory = _ => 320,
    };

    private readonly Option<int> _height = new("--height", "-H")
    {
        Description = "Thumbnail height in pixels (0 = derived from aspect ratio).",
        DefaultValueFactory = _ => 0,
    };

    private readonly Option<string?> _aspect = new("--aspect", "-A")
    {
        Description = "Aspect ratio override: float (1.778) or fraction (16/9).",
    };

    // ── Time options ─────────────────────────────────────────────────────────────
    private readonly Option<TimeIndex?> _interval = TimeIndexOption("--interval", "-i", "Capture interval (e.g. 3m30, 90, 1:22).");

    private readonly Option<TimeIndex?> _from = TimeIndexOption("--from", "Start time.");

    private readonly Option<TimeIndex?> _to = TimeIndexOption("--to", "-t", "End time.");

    // ── Output options ───────────────────────────────────────────────────────────
    private readonly Option<string[]> _output = new("--output", "-o")
    {
        Description = "Output file (repeatable, paired with inputs).",
        Arity = ArgumentArity.ZeroOrMore,
    };

    private readonly Option<string> _format = CreateFormatOption();

    // ── Text / metadata options ──────────────────────────────────────────────────
    private readonly Option<string?> _title = new("--title", "-T") { Description = "Sheet title." };

    private readonly Option<string?> _signature = new("--signature", "-s") { Description = "Footer signature text." };

    private readonly Option<bool> _noSignature = new("--no-signature") { Description = "Remove footer signature." };

    // ── Highlight frames ─────────────────────────────────────────────────────────
    private readonly Option<string[]> _highlight = new("--highlight", "-l")
    {
        Description = "Add a highlight frame at TIME (repeatable).",
        Arity = ArgumentArity.ZeroOrMore,
    };

    // ── Style toggles ────────────────────────────────────────────────────────────
    private readonly Option<bool> _timestamp = new("--timestamp")
    {
        Description = "Enable the caption overlay: timestamp for video, file name for images (default: on).",
    };

    private readonly Option<bool> _noTimestamp = new("--no-timestamp") { Description = "Disable the caption overlay." };

    private readonly Option<bool> _polaroid = new("--polaroid") { Description = "Enable polaroid frame (default: off)." };

    private readonly Option<bool> _noPolaroid = new("--no-polaroid") { Description = "Disable polaroid frame." };

    private readonly Option<bool> _shadow = new("--shadow") { Description = "Enable drop shadow (default: on)." };

    private readonly Option<bool> _noShadow = new("--no-shadow") { Description = "Disable drop shadow." };

    // ── Image options ────────────────────────────────────────────────────────────
    private readonly Option<bool> _recursive = new("--recursive")
    {
        Description = "Include images in subfolders (image folders only).",
    };

    private readonly Option<string?> _fit = CreateFitOption();

    private readonly Option<bool> _allImages = new("--all")
    {
        Description = "Render every image instead of sampling --columns x --rows of them (image inputs only).",
    };

    // ── Config / tool / runtime options ─────────────────────────────────────────
    private readonly Option<string?> _config = new("--config")
    {
        Description = "Path to a TOML config file (layered on top of auto-discovered configs).",
    };

    private readonly Option<string?> _ffmpegFolder = new("--ffmpeg-folder")
    {
        Description = "Folder containing ffmpeg/ffprobe binaries (default: bundled or PATH).",
    };

    private readonly Option<bool> _quiet = new("--quiet", "-q") { Description = "Only print errors." };

    private readonly Option<bool> _continue = new("--continue") { Description = "Continue with next file on error." };

    /// <summary>Builds the root command containing every option declared above.</summary>
    public RootCommand BuildRootCommand()
        => new("Virtual Contact Sheet — generates contact sheets from video files or from folders of images.")
        {
            _files,
            _columns, _rows, _width, _height, _aspect,
            _interval, _from, _to,
            _output, _format,
            _title, _signature, _noSignature,
            _highlight,
            _timestamp, _noTimestamp,
            _polaroid, _noPolaroid,
            _shadow, _noShadow,
            _recursive, _fit, _allImages,
            _config, _ffmpegFolder, _quiet, _continue,
        };

    /// <summary>Projects a parsed command line onto a <see cref="CliSettings"/>.</summary>
    public CliSettings Bind(ParseResult parse) => new()
    {
        Files = parse.GetValue(_files)!,
        Outputs = parse.GetValue(_output) ?? [],
        Interval = parse.GetValue(_interval),
        From = parse.GetValue(_from),
        To = parse.GetValue(_to),

        // Null when not explicitly provided — factory falls back to config then library defaults.
        Columns = parse.GetResult(_columns) is not null ? parse.GetValue(_columns) : null,
        Rows = parse.GetResult(_rows) is not null ? parse.GetValue(_rows) : null,
        Width = parse.GetResult(_width) is not null ? parse.GetValue(_width) : null,
        Height = parse.GetValue(_height),
        AspectRatio = parse.GetValue(_aspect),
        Format = parse.GetResult(_format) is not null ? parse.GetValue(_format) : null,
        Title = parse.GetValue(_title),
        Signature = parse.GetValue(_signature),
        NoSignature = parse.GetValue(_noSignature),
        HighlightStrings = parse.GetValue(_highlight) ?? [],
        UseTimestamp = parse.GetValue(_timestamp),
        NoTimestamp = parse.GetValue(_noTimestamp),
        UsePolaroid = parse.GetValue(_polaroid),
        NoPolaroid = parse.GetValue(_noPolaroid),
        UseShadow = parse.GetValue(_shadow),
        NoShadow = parse.GetValue(_noShadow),
        Recursive = parse.GetValue(_recursive),
        Fit = parse.GetResult(_fit) is not null ? parse.GetValue(_fit) : null,
        AllImages = parse.GetValue(_allImages),
        ConfigPath = parse.GetValue(_config),
        FfmpegFolder = parse.GetValue(_ffmpegFolder) ?? BundledBinaries.Detect(),
        Quiet = parse.GetValue(_quiet),
        ContinueOnError = parse.GetValue(_continue),
    };

    private static Option<string> CreateFormatOption()
    {
        var opt = new Option<string>("--format", "-f")
        {
            Description = "Output format: png, jpg, jpeg, webp.",
            DefaultValueFactory = _ => "png",
        };
        opt.AcceptOnlyFromAmong(Formats.Accepted);
        return opt;
    }

    private static Option<string?> CreateFitOption()
    {
        var opt = new Option<string?>("--fit")
        {
            Description = "How images that do not match the cell shape are fitted: contain, cover, stretch.",
        };
        opt.AcceptOnlyFromAmong(ImageOptionsFactory.AcceptedFits);
        return opt;
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
}
