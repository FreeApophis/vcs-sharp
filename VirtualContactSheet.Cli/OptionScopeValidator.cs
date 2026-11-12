namespace VirtualContactSheet.Cli;

/// <summary>
/// Rejects options that cannot apply to any of the resolved inputs — <c>--interval</c> on a
/// folder of photos, <c>--fit</c> on a video. A run that mixes both kinds is fine: each option
/// applies to the inputs it belongs to, so nothing is reported there.
/// </summary>
internal static class OptionScopeValidator
{
    /// <summary>The inputs unchanged, or the error for the first option that applies to none of them.</summary>
    public static Either<string, IReadOnlyList<SheetInput>> Validate(CliSettings settings, IReadOnlyList<SheetInput> inputs)
        => FirstMisapplied(settings, inputs).Match(
            none: () => Either<string, IReadOnlyList<SheetInput>>.Right(inputs),
            some: Either<string, IReadOnlyList<SheetInput>>.Left);

    private static Option<string> FirstMisapplied(CliSettings settings, IReadOnlyList<SheetInput> inputs)
        => Misapplied(VideoOnly(settings), "video", applies: inputs.Any(input => input is VideoInput))
            .OrElse(() => Misapplied(ImageOnly(settings), "image", applies: inputs.Any(input => input is ImageInput)));

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
        ("--layout", settings.Layout is not null),
    ];

    private static Option<string> Misapplied(IEnumerable<(string Name, bool Used)> options, string medium, bool applies)
        => applies
            ? Option<string>.None
            : options.FirstOrNone(option => option.Used).Select(option => $"{option.Name} applies to {medium} inputs only.");
}
