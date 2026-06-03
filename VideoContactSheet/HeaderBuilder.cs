namespace VideoContactSheet;

/// <summary>
/// Builds the two-column metadata header, mirroring the original vcs.rb layout:
/// left column = filename / file size / length, right column = dimensions / format / fps.
/// </summary>
public static class HeaderBuilder
{
    public static HeaderColumns Build(string fileName, VideoInfo info)
    {
        var v = info.Video;
        var a = info.Audio;

        var left = new List<string>
        {
            $"Filename: {fileName}",
            $"File size: {FormatBytes(info.FileSize)}",
            $"Length: {FormatDuration(info.Duration)}",
        };

        var right = new List<string>();
        if (v is not null)
        {
            right.Add($"Dimensions: {v.Width}x{v.Height}");
        }

        var format = FormatCodecs(v, a);
        if (format is not null)
        {
            right.Add($"Format: {format}");
        }

        if (v?.FrameRate is { } fps)
        {
            right.Add($"FPS: {fps:0.##}");
        }

        return new HeaderColumns(left, right);
    }

    /// <summary>"h264 (High) / aac" — video codec (with profile) and audio codec.</summary>
    private static string? FormatCodecs(VideoStream? v, AudioStream? a)
    {
        var parts = new List<string>();
        if (v?.Codec is { } videoCodec)
        {
            var profile = !string.IsNullOrEmpty(v.Profile) ? $" ({v.Profile})" : string.Empty;
            parts.Add($"{videoCodec}{profile}");
        }

        if (a?.Codec is { } audioCodec)
        {
            parts.Add(audioCodec);
        }

        return parts.Count > 0 ? string.Join(" / ", parts) : null;
    }

    private static string FormatDuration(TimeSpan d)
        => d.TotalHours >= 1
            ? $"{(int)d.TotalHours}:{d.Minutes:D2}:{d.Seconds:D2}"
            : $"{d.Minutes:D2}:{d.Seconds:D2}";

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KiB", "MiB", "GiB", "TiB" };
        double size = bytes;
        int u = 0;
        while (size >= 1024 && u < units.Length - 1)
        {
            size /= 1024;
            u++;
        }

        return $"{size:0.##} {units[u]}";
    }
}
