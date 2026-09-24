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

    public static bool TryResolve(IReadOnlyList<string> arguments, out IReadOnlyList<SheetInput> inputs, out string? error)
    {
        var resolved = new List<SheetInput>();
        var loose = new List<string>();
        int looseSlot = -1;

        foreach (var argument in arguments)
        {
            if (!TryExpand(argument, out var paths, out error))
            {
                inputs = [];
                return false;
            }

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    resolved.Add(FolderInput(path));
                }
                else if (ImageCollection.IsSupported(path))
                {
                    // Reserve this slot on the first loose image; the rest join that same group.
                    if (looseSlot < 0)
                    {
                        looseSlot = resolved.Count;
                        resolved.Add(null!);
                    }

                    loose.Add(path);
                }
                else
                {
                    resolved.Add(VideoInput(path));
                }
            }
        }

        if (looseSlot >= 0)
        {
            resolved[looseSlot] = LooseImagesInput(loose);
        }

        inputs = resolved;
        error = null;
        return true;
    }

    private static SheetInput VideoInput(string path) => new()
    {
        Kind = SheetInputKind.Video,
        Paths = [path],
        DisplayName = path,
        OutputBase = StripExtension(path),
    };

    private static SheetInput FolderInput(string folder)
    {
        // "photos/holiday" and "photos/holiday/" must both yield "photos/holiday.png".
        var trimmed = Path.TrimEndingDirectorySeparator(folder);

        return new SheetInput
        {
            Kind = SheetInputKind.Images,
            Paths = [trimmed],
            Folder = trimmed,
            DisplayName = trimmed,
            OutputBase = trimmed,
        };
    }

    private static SheetInput LooseImagesInput(IReadOnlyList<string> paths) => new()
    {
        Kind = SheetInputKind.Images,
        Paths = paths,
        DisplayName = paths.Count == 1 ? paths[0] : $"{paths.Count} images",

        // A group of files has no name of its own, so it lands in the working directory.
        OutputBase = paths.Count == 1 ? StripExtension(paths[0]) : LooseGroupOutputName,
    };

    private static string StripExtension(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Length == 0 ? path : path[..^extension.Length];
    }

    /// <summary>
    /// Expands a wildcard argument. cmd.exe and PowerShell hand wildcards through verbatim, unlike
    /// a POSIX shell, so the CLI expands them itself to behave the same on every platform.
    /// </summary>
    private static bool TryExpand(string argument, out IReadOnlyList<string> paths, out string? error)
    {
        error = null;

        if (!argument.Contains('*') && !argument.Contains('?'))
        {
            paths = [argument];
            return true;
        }

        var directory = Path.GetDirectoryName(argument);
        var pattern = Path.GetFileName(argument);

        if (pattern.Length == 0)
        {
            paths = [];
            error = $"Invalid path pattern: '{argument}'.";
            return false;
        }

        var searchRoot = string.IsNullOrEmpty(directory) ? "." : directory;
        if (!Directory.Exists(searchRoot))
        {
            paths = [];
            error = $"No files matched '{argument}'.";
            return false;
        }

        var matches = Directory
            .EnumerateFileSystemEntries(searchRoot, pattern)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (matches.Count == 0)
        {
            paths = [];
            error = $"No files matched '{argument}'.";
            return false;
        }

        // Keep the caller's spelling: a bare pattern must not gain a "./" prefix.
        paths = string.IsNullOrEmpty(directory)
            ? [.. matches.Select(match => Path.GetFileName(match)!)]
            : matches;

        return true;
    }
}
