namespace VideoContactSheet.Cli;

/// <summary>
/// Translates parsed <see cref="CliSettings"/> into a <see cref="ContactSheetOptions"/>,
/// starting from library defaults and applying only the values the user actually set.
/// </summary>
internal static class ContactSheetOptionsFactory
{
    public static bool TryCreate(CliSettings settings, out ContactSheetOptions options, out string? error)
    {
        var result = new ContactSheetOptions
        {
            Columns = settings.Columns,
            Rows = settings.Rows,
            ThumbnailWidth = settings.Width,
            Format = Formats.Parse(settings.Format),
        };

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

        options = result;
        error = null;
        return true;
    }

    /// <summary>
    /// Applies a paired <c>--x</c> / <c>--no-x</c> flag. The negative flag wins, then the
    /// positive flag; if neither is set the library default is left untouched.
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
