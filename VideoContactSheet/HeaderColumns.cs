namespace VideoContactSheet;

/// <summary>
/// Header band content laid out as two columns: <see cref="Left"/> lines are drawn
/// left-aligned, <see cref="Right"/> lines right-aligned, row by row.
/// </summary>
public sealed record HeaderColumns(IReadOnlyList<string> Left, IReadOnlyList<string> Right)
{
    public static HeaderColumns Empty { get; } = new(Array.Empty<string>(), Array.Empty<string>());
}
