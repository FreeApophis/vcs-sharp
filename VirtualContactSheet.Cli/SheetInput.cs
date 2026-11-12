namespace VirtualContactSheet.Cli;

/// <summary>
/// One unit of work — exactly one output sheet. Closed, so every <c>switch</c> over it is checked
/// for exhaustiveness: a new kind of input fails the build until each consumer handles it.
/// </summary>
/// <param name="DisplayName">What the progress line calls this input.</param>
/// <param name="OutputBase">Output path without an extension; the chosen format appends one.</param>
internal closed record SheetInput(string DisplayName, string OutputBase);

/// <summary>A single video file.</summary>
internal sealed record VideoInput(string Path, string DisplayName, string OutputBase)
    : SheetInput(DisplayName, OutputBase);

/// <summary>An image sheet, built from either a folder or a group of loose files.</summary>
internal closed record ImageInput(string DisplayName, string OutputBase)
    : SheetInput(DisplayName, OutputBase);

/// <summary>A folder of images; whether it is searched recursively is an image option.</summary>
internal sealed record ImageFolderInput(string Folder, string DisplayName, string OutputBase)
    : ImageInput(DisplayName, OutputBase);

/// <summary>Loose image files from the command line, coalesced into one sheet in the given order.</summary>
internal sealed record LooseImagesInput(IReadOnlyList<string> Paths, string DisplayName, string OutputBase)
    : ImageInput(DisplayName, OutputBase);
