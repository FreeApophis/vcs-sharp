using System.Globalization;

namespace VideoContactSheet.Cli;

/// <summary>
/// Translates parsed <see cref="CliSettings"/> and <see cref="VcsConfig"/> into a
/// <see cref="ContactSheetOptions"/>, starting from library defaults, then layering config,
/// then CLI args (which always win).
/// </summary>
internal static class ContactSheetOptionsFactory
{
    public static bool TryCreate(CliSettings settings, VcsConfig config, out ContactSheetOptions options, out string? error)
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

        if (!TryParseHighlights(settings.HighlightStrings, result, out error))
        {
            options = result;
            return false;
        }

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

        if (settings.Height > 0)
        {
            result.ThumbnailHeight = settings.Height;
        }

        if (settings.AspectRatio is not null)
        {
            if (!TryParseAspectRatio(settings.AspectRatio, out var ar))
            {
                error = $"Invalid aspect ratio '{settings.AspectRatio}'. Expected a number (1.778) or fraction (16/9).";
                options = result;
                return false;
            }

            result.AspectRatio = ar;
        }

        options = result;
        error = null;
        return true;
    }

    /// <summary>Convenience overload used by tests; equivalent to an empty config.</summary>
    public static bool TryCreate(CliSettings settings, out ContactSheetOptions options, out string? error)
        => TryCreate(settings, new VcsConfig(), out options, out error);

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

            if (main.Interval is { } interval && TimeIndex.TryParse(interval, out var ti))
            {
                result.Interval = ti;
            }
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

            if (style.Contact?.Background is { } contactBg && ColorParser.TryParse(contactBg, out var sheetColor))
            {
                result.SheetBackground = sheetColor;
            }

            if (style.Highlight?.Background is { } hlBg && ColorParser.TryParse(hlBg, out var hlColor))
            {
                result.HighlightBackground = hlColor;
            }
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

        if (config.Color is { } color && ColorParser.TryParse(color, out var c))
        {
            target.Color = c;
        }

        if (config.Background is { } bg && ColorParser.TryParse(bg, out var bc))
        {
            target.Background = bc;
        }
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

    private static bool TryParseAspectRatio(string input, out float ratio)
    {
        int slash = input.IndexOf('/');
        if (slash >= 0)
        {
            if (float.TryParse(input[..slash], NumberStyles.Float, CultureInfo.InvariantCulture, out float num)
                && float.TryParse(input[(slash + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture, out float den)
                && den != 0)
            {
                ratio = num / den;
                return ratio > 0;
            }

            ratio = 0;
            return false;
        }

        return float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out ratio) && ratio > 0;
    }

    private static bool TryParseHighlights(IReadOnlyList<string> highlightStrings, ContactSheetOptions options, out string? error)
    {
        foreach (var h in highlightStrings)
        {
            if (!TimeIndex.TryParse(h, out var ti))
            {
                error = $"Invalid highlight time: '{h}'";
                return false;
            }

            options.Highlights.Add(ti);
        }

        error = null;
        return true;
    }
}
