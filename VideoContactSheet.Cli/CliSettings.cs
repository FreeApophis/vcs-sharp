namespace VideoContactSheet.Cli;

/// <summary>
/// Strongly-typed bag of all CLI values after parsing. Produced by
/// <see cref="CliOptions.Bind"/> so that the rest of the pipeline never touches
/// <c>ParseResult</c> directly and can be unit-tested in isolation.
/// </summary>
internal sealed record CliSettings
{
    public required string[] Files { get; init; }

    public required string[] Outputs { get; init; }

    public TimeIndex? Interval { get; init; }

    public TimeIndex? From { get; init; }

    public TimeIndex? To { get; init; }

    public int Columns { get; init; }

    public int Rows { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public string? AspectRatio { get; init; }

    public required string Format { get; init; }

    public string? Title { get; init; }

    public string? Signature { get; init; }

    public bool NoSignature { get; init; }

    public required IReadOnlyList<string> HighlightStrings { get; init; }

    public bool UseTimestamp { get; init; }

    public bool NoTimestamp { get; init; }

    public bool UsePolaroid { get; init; }

    public bool NoPolaroid { get; init; }

    public bool UseShadow { get; init; }

    public bool NoShadow { get; init; }

    public string? FfmpegFolder { get; init; }

    public bool Quiet { get; init; }

    public bool ContinueOnError { get; init; }
}
