namespace VirtualContactSheet.Cli;

/// <summary>Which pipeline produces a sheet for an input.</summary>
internal enum SheetInputKind
{
    Video,
    Images,
}

/// <summary>
/// One unit of work — exactly one output sheet. A video input holds a single path; an image
/// input holds either a folder or the loose image files that were grouped into one sheet.
/// </summary>
internal sealed record SheetInput
{
    public required SheetInputKind Kind { get; init; }

    /// <summary>The video file, the folder, or the loose image files.</summary>
    public required IReadOnlyList<string> Paths { get; init; }

    /// <summary>Set when this is a folder of images; null for loose files and for video.</summary>
    public string? Folder { get; init; }

    /// <summary>What the progress line calls this input.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Output path without an extension; the chosen format appends one.</summary>
    public required string OutputBase { get; init; }
}
