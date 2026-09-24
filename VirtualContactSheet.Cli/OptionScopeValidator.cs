namespace VirtualContactSheet.Cli;

/// <summary>
/// Rejects options that cannot apply to any of the resolved inputs — <c>--interval</c> on a
/// folder of photos, <c>--fit</c> on a video. A run that mixes both kinds is fine: each option
/// applies to the inputs it belongs to, so nothing is reported there.
/// </summary>
internal static class OptionScopeValidator
{
    public static bool Validate(CliSettings settings, IReadOnlyList<SheetInput> inputs, out string? error)
    {
        bool hasVideo = inputs.Any(input => input.Kind == SheetInputKind.Video);
        bool hasImages = inputs.Any(input => input.Kind == SheetInputKind.Images);

        if (!hasVideo && Misapplied(VideoOnly(settings)) is { } videoOption)
        {
            error = $"{videoOption} applies to video inputs only.";
            return false;
        }

        if (!hasImages && Misapplied(ImageOnly(settings)) is { } imageOption)
        {
            error = $"{imageOption} applies to image inputs only.";
            return false;
        }

        error = null;
        return true;
    }

    private static IEnumerable<(string Name, bool Used)> VideoOnly(CliSettings settings) =>
    [
        ("--interval", settings.Interval.HasValue),
        ("--from", settings.From.HasValue),
        ("--to", settings.To.HasValue),
        ("--highlight", settings.HighlightStrings.Count > 0),
    ];

    private static IEnumerable<(string Name, bool Used)> ImageOnly(CliSettings settings) =>
    [
        ("--fit", settings.Fit is not null),
        ("--recursive", settings.Recursive),
        ("--all", settings.AllImages),
    ];

    private static string? Misapplied(IEnumerable<(string Name, bool Used)> options)
        => options.FirstOrDefault(option => option.Used).Name;
}
