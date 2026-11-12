using System.Globalization;

namespace VirtualContactSheet.Cli;

/// <summary>
/// Translates parsed <see cref="CliSettings"/> and <see cref="VcsConfig"/> into a
/// <see cref="ContactSheetOptions"/>, starting from library defaults, then layering config,
/// then CLI args (which always win).
/// </summary>
internal static class ContactSheetOptionsFactory
{
    /// <summary>
    /// The sheet options, or the error for the first invalid <c>--highlight</c>, <c>--aspect</c>
    /// or <c>--padding</c>.
    /// </summary>
    public static Either<string, ContactSheetOptions> Create(CliSettings settings, VcsConfig config)
        => from highlights in ParseHighlights(settings.HighlightStrings)
           from aspectRatio in ParseAspectRatioSetting(settings.AspectRatio)
           from padding in ValidatePadding(settings.Padding)
           select Build(settings, config, highlights, aspectRatio, padding);

    /// <summary>Convenience overload used by tests; equivalent to an empty config.</summary>
    public static Either<string, ContactSheetOptions> Create(CliSettings settings)
        => Create(settings, new VcsConfig());

    private static ContactSheetOptions Build(
        CliSettings settings,
        VcsConfig config,
        IReadOnlyList<TimeIndex> highlights,
        Option<float> aspectRatio,
        Option<int> padding)
    {
        var result = new ContactSheetOptions();

        ApplyConfig(result, config);

        // CLI args override config — only applied when the option was explicitly provided.
        if (settings.Columns is { } columns)
        {
            result.Columns = columns;
        }

        if (settings.Rows is { } rows)
        {
            result.Rows = rows;
        }

        if (settings.Width is { } width)
        {
            result.ThumbnailWidth = width;
        }

        if (settings.Format is { } format)
        {
            result.Format = Formats.Parse(format);
        }

        result.Highlights.AddRange(highlights);

        if (settings.Interval.HasValue)
        {
            result.Interval = settings.Interval;
        }

        if (settings.From.HasValue)
        {
            result.From = settings.From;
        }

        if (settings.To.HasValue)
        {
            result.To = settings.To;
        }

        if (settings.Title is not null)
        {
            result.Title = settings.Title;
        }

        if (settings.Signature is not null)
        {
            result.Signature = settings.Signature;
        }

        if (settings.NoSignature)
        {
            result.ShowSignature = false;
        }

        ApplyToggle(settings.UseTimestamp, settings.NoTimestamp, on => result.Timestamp = on);
        ApplyToggle(settings.UsePolaroid, settings.NoPolaroid, on => result.Polaroid = on);
        ApplyToggle(settings.UseShadow, settings.NoShadow, on => result.SoftShadow = on);
        ApplyToggle(settings.UseHeader, settings.NoHeader, on => result.ShowHeader = on);

        if (settings.Height > 0)
        {
            result.ThumbnailHeight = settings.Height;
        }

        result.AspectRatio = aspectRatio.GetOrElse(result.AspectRatio);
        result.Padding = padding.GetOrElse(result.Padding);

        return result;
    }

    private static void ApplyConfig(ContactSheetOptions result, VcsConfig config)
    {
        if (config.Main is { } main)
        {
            if (main.Columns is { } cols)
            {
                result.Columns = cols;
            }

            if (main.Rows is { } rows)
            {
                result.Rows = rows;
            }

            if (main.Padding is { } pad)
            {
                result.Padding = pad;
            }

            if (main.Quality is { } q)
            {
                result.JpegQuality = q;
            }

            if (main.Width is { } w)
            {
                result.ThumbnailWidth = w;
            }

            // An unparseable config interval is ignored, like an invalid config color.
            result.Interval = Option.FromNullable(main.Interval).SelectMany(ParseTimeIndex).Match(none: result.Interval, some: interval => interval);
        }

        if (config.Filter is { } filter)
        {
            if (filter.Timestamp is { } ts)
            {
                result.Timestamp = ts;
            }

            if (filter.Polaroid is { } pol)
            {
                result.Polaroid = pol;
            }

            if (filter.Shadow is { } shadow)
            {
                result.SoftShadow = shadow;
            }

            if (filter.Header is { } header)
            {
                result.ShowHeader = header;
            }

            if (filter.BlankEvasion is { } be)
            {
                result.BlankEvasion = be;
            }

            if (filter.BlankThreshold is { } bt)
            {
                result.BlankThreshold = bt;
            }
        }

        if (config.Style is { } style)
        {
            ApplyTextStyle(style.Header, result.HeaderStyle);
            ApplyTextStyle(style.Title, result.TitleStyle);
            ApplyTextStyle(style.Timestamp, result.TimestampStyle);
            ApplyTextStyle(style.Signature, result.SignatureStyle);

            result.SheetBackground = ColorParser.Parse(style.Contact?.Background).GetOrElse(result.SheetBackground);
            result.HighlightBackground = ColorParser.Parse(style.Highlight?.Background).GetOrElse(result.HighlightBackground);
        }
    }

    private static void ApplyTextStyle(TextStyleConfig? config, TextStyle target)
    {
        if (config is null)
        {
            return;
        }

        if (config.Size is { } size)
        {
            target.Size = size;
        }

        if (config.Bold is { } bold)
        {
            target.Bold = bold;
        }

        if (config.FontFamily is { } family)
        {
            target.FontFamily = family;
        }

        if (config.FontFile is { } file)
        {
            target.FontFile = file;
        }

        target.Color = ColorParser.Parse(config.Color).GetOrElse(target.Color);
        target.Background = ColorParser.Parse(config.Background).GetOrElse(target.Background);
    }

    /// <summary>
    /// Applies a paired <c>--x</c> / <c>--no-x</c> flag. The negative flag wins, then the
    /// positive flag; if neither is set the config/library default is left untouched.
    /// </summary>
    private static void ApplyToggle(bool enable, bool disable, Action<bool> set)
    {
        if (disable)
        {
            set(false);
        }
        else if (enable)
        {
            set(true);
        }
    }

    /// <summary>None when <c>--padding</c> was not given; an error when it is negative.</summary>
    private static Either<string, Option<int>> ValidatePadding(int? value)
        => Option.FromNullable(value).Traverse(padding => padding >= 0
            ? Either<string, int>.Right(padding)
            : Either<string, int>.Left($"Invalid --padding {padding}: must be 0 or more."));

    private static Either<string, IReadOnlyList<TimeIndex>> ParseHighlights(IReadOnlyList<string> values)
        => values.Traverse(value => ParseTimeIndex(value).ToEither($"Invalid highlight time: '{value}'"));

    /// <summary>None when <c>--aspect</c> was not given; an error when it was given but is invalid.</summary>
    private static Either<string, Option<float>> ParseAspectRatioSetting(string? value)
        => Option.FromNullable(value).Traverse(text => ParseAspectRatio(text)
            .ToEither($"Invalid aspect ratio '{text}'. Expected a number (1.778) or fraction (16/9)."));

    /// <summary>A positive number (<c>1.778</c>) or fraction (<c>16/9</c>).</summary>
    private static Option<float> ParseAspectRatio(string text)
        => ParseNumberOrFraction(text).Where(ratio => ratio > 0);

    private static Option<float> ParseNumberOrFraction(string text)
        => text.Split('/') switch
        {
            [var number] => ParseFloat(number),
            [var numerator, var denominator] =>
                from n in ParseFloat(numerator)
                from d in ParseFloat(denominator).Where(d => d != 0)
                select n / d,
            _ => Option<float>.None,
        };

    private static Option<float> ParseFloat(string text)
        => text.ParseSingleOrNone(NumberStyles.Float, CultureInfo.InvariantCulture);

    private static Option<TimeIndex> ParseTimeIndex(string text)
        => TimeIndex.TryParse(text, out var time) ? time : Option<TimeIndex>.None;
}
