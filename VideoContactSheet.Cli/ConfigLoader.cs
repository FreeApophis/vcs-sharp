using System.Text.Json;
using Tomlyn;

namespace VideoContactSheet.Cli;

internal static class ConfigLoader
{
    private const string FileName = ".vcs.toml";

    public static VcsConfig Load(string? explicitPath)
    {
        var result = new VcsConfig();

        foreach (var (path, required) in DiscoverPaths(explicitPath))
        {
            if (!File.Exists(path))
            {
                if (required)
                {
                    throw new FileNotFoundException($"Config file not found: '{path}'");
                }

                continue;
            }

            var layer = ParseFile(path);
            Merge(result, layer);
        }

        return result;
    }

    private static IEnumerable<(string Path, bool Required)> DiscoverPaths(string? explicitPath)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        yield return (Path.Combine(appData, "vcs", "config.toml"), false);

        yield return (Path.Combine(Directory.GetCurrentDirectory(), FileName), false);

        if (explicitPath is not null)
        {
            yield return (explicitPath, true);
        }
    }

    private static VcsConfig ParseFile(string path)
    {
        var text = File.ReadAllText(path);
        var options = new TomlSerializerOptions
        {
            // Map CLR PascalCase to snake_case TOML keys (e.g. BlankThreshold -> blank_threshold).
            // Unknown keys are ignored by default in Tomlyn 2.x.
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };

        try
        {
            return TomlSerializer.Deserialize<VcsConfig>(text, options) ?? new VcsConfig();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse config '{path}': {ex.Message}", ex);
        }
    }

    private static void Merge(VcsConfig target, VcsConfig source)
    {
        if (source.Main is { } main)
        {
            target.Main ??= new();
            target.Main.Columns = main.Columns ?? target.Main.Columns;
            target.Main.Rows = main.Rows ?? target.Main.Rows;
            target.Main.Padding = main.Padding ?? target.Main.Padding;
            target.Main.Quality = main.Quality ?? target.Main.Quality;
            target.Main.Width = main.Width ?? target.Main.Width;
            target.Main.Interval = main.Interval ?? target.Main.Interval;
        }

        if (source.Filter is { } filter)
        {
            target.Filter ??= new();
            target.Filter.Timestamp = filter.Timestamp ?? target.Filter.Timestamp;
            target.Filter.Polaroid = filter.Polaroid ?? target.Filter.Polaroid;
            target.Filter.Shadow = filter.Shadow ?? target.Filter.Shadow;
            target.Filter.BlankEvasion = filter.BlankEvasion ?? target.Filter.BlankEvasion;
            target.Filter.BlankThreshold = filter.BlankThreshold ?? target.Filter.BlankThreshold;
        }

        if (source.Style is { } style)
        {
            target.Style ??= new();
            target.Style.Header = MergeTextStyle(target.Style.Header, style.Header);
            target.Style.Title = MergeTextStyle(target.Style.Title, style.Title);
            target.Style.Timestamp = MergeTextStyle(target.Style.Timestamp, style.Timestamp);
            target.Style.Signature = MergeTextStyle(target.Style.Signature, style.Signature);
            target.Style.Contact = MergeBackground(target.Style.Contact, style.Contact);
            target.Style.Highlight = MergeBackground(target.Style.Highlight, style.Highlight);
        }
    }

    private static TextStyleConfig? MergeTextStyle(TextStyleConfig? target, TextStyleConfig? source)
    {
        if (source is null)
        {
            return target;
        }

        if (target is null)
        {
            return source;
        }

        return new TextStyleConfig
        {
            Size = source.Size ?? target.Size,
            Color = source.Color ?? target.Color,
            Background = source.Background ?? target.Background,
            FontFamily = source.FontFamily ?? target.FontFamily,
            FontFile = source.FontFile ?? target.FontFile,
            Bold = source.Bold ?? target.Bold,
        };
    }

    private static BackgroundConfig? MergeBackground(BackgroundConfig? target, BackgroundConfig? source)
    {
        if (source is null)
        {
            return target;
        }

        if (target is null)
        {
            return source;
        }

        return new BackgroundConfig { Background = source.Background ?? target.Background };
    }
}
