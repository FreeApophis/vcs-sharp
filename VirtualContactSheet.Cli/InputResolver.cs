using VirtualContactSheet.ImageProcessing;

namespace VirtualContactSheet.Cli;

/// <summary>
/// Turns the positional arguments into the list of sheets to produce.
///
/// A directory and a video each make their own sheet, but N image files make <em>one</em> sheet,
/// so loose image files are coalesced into a single group that takes the position of the first
/// one. Detection order is directory, then a known image extension, then video — so anything that
/// worked before the image support landed still resolves to video, including unusual containers.
/// </summary>
internal static class InputResolver
{
    private const string LooseGroupOutputName = "contact-sheet";

    /// <summary>The sheets to produce, or the error for the first argument that cannot be expanded.</summary>
    public static Either<string, IReadOnlyList<SheetInput>> Resolve(IReadOnlyList<string> arguments)
        => arguments
            .Traverse(Expand)
            .Select(expanded => Coalesce([.. expanded.SelectMany(paths => paths).Select(Classify)]));

    /// <summary>Directory, then a known image extension, then video; a loose image starts as a group of one.</summary>
    private static SheetInput Classify(string path)
        => path switch
        {
            _ when Directory.Exists(path) => ForFolder(path),
            _ when ImageCollection.IsSupported(path) => ForLooseImages([path]),
            _ => ForVideo(path),
        };

    /// <summary>Merges every loose image into one group, placed where the first of them appeared.</summary>
    private static IReadOnlyList<SheetInput> Coalesce(List<SheetInput> classified)
    {
        var loose = classified.OfType<LooseImagesInput>().SelectMany(single => single.Paths).ToList();
        var firstLoose = classified.FindIndex(input => input is LooseImagesInput);

        return [.. classified.WhereSelect((input, index) => Place(input, index == firstLoose, loose))];
    }

    /// <summary>Keeps videos and folders; the first loose image becomes the whole group, the rest drop out.</summary>
    private static Option<SheetInput> Place(SheetInput input, bool isFirstLoose, IReadOnlyList<string> loose)
        => input switch
        {
            LooseImagesInput when isFirstLoose => ForLooseImages(loose),
            LooseImagesInput => Option<SheetInput>.None,
            VideoInput or ImageFolderInput => input,
        };

    private static VideoInput ForVideo(string path)
        => new(Path: path, DisplayName: path, OutputBase: StripExtension(path));

    private static ImageFolderInput ForFolder(string folder)
    {
        // "photos/holiday" and "photos/holiday/" must both yield "photos/holiday.png".
        var trimmed = Path.TrimEndingDirectorySeparator(folder);

        return new(Folder: trimmed, DisplayName: trimmed, OutputBase: trimmed);
    }

    private static LooseImagesInput ForLooseImages(IReadOnlyList<string> paths)
        => new(
            Paths: paths,
            DisplayName: paths.Count == 1 ? paths[0] : $"{paths.Count} images",

            // A group of files has no name of its own, so it lands in the working directory.
            OutputBase: paths.Count == 1 ? StripExtension(paths[0]) : LooseGroupOutputName);

    private static string StripExtension(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Length == 0 ? path : path[..^extension.Length];
    }

    /// <summary>
    /// Expands a wildcard argument. cmd.exe and PowerShell hand wildcards through verbatim, unlike
    /// a POSIX shell, so the CLI expands them itself to behave the same on every platform.
    /// </summary>
    private static Either<string, IReadOnlyList<string>> Expand(string argument)
    {
        if (!argument.Contains('*') && !argument.Contains('?'))
        {
            return Paths([argument]);
        }

        var directory = Path.GetDirectoryName(argument);
        var pattern = Path.GetFileName(argument);

        if (pattern.Length == 0)
        {
            return Error($"Invalid path pattern: '{argument}'.");
        }

        var searchRoot = string.IsNullOrEmpty(directory) ? "." : directory;
        if (!Directory.Exists(searchRoot))
        {
            return NoMatch(argument);
        }

        var matches = Directory
            .EnumerateFileSystemEntries(searchRoot, pattern)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (matches.Count == 0)
        {
            return NoMatch(argument);
        }

        // Keep the caller's spelling: a bare pattern must not gain a "./" prefix.
        return Paths(string.IsNullOrEmpty(directory)
            ? [.. matches.Select(match => Path.GetFileName(match)!)]
            : matches);
    }

    private static Either<string, IReadOnlyList<string>> Paths(IReadOnlyList<string> paths)
        => Either<string, IReadOnlyList<string>>.Right(paths);

    private static Either<string, IReadOnlyList<string>> Error(string message)
        => Either<string, IReadOnlyList<string>>.Left(message);

    private static Either<string, IReadOnlyList<string>> NoMatch(string argument)
        => Error($"No files matched '{argument}'.");
}
