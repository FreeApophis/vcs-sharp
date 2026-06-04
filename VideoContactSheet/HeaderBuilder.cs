namespace VideoContactSheet;

/// <summary>
/// Builds the two-column metadata header, mirroring the original vcs.rb layout:
/// left column = filename / file size / length, right column = dimensions / format / fps.
/// </summary>
public static class HeaderBuilder
{
    public static HeaderColumns Build(string fileName, VideoInfo info)
        => new(LeftColumn(fileName, info), RightColumn(info));

    private static List<string> RightColumn(VideoInfo info)
    {
        var v = info.Video;
        var a = info.Audio;

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

        return right;
    }

    private static List<string> LeftColumn(string fileName, VideoInfo info)
        =>
        [
            $"Filename: {fileName}",
            $"File size: {info.FileSize.FormatBytes()}",
            $"Length: {FormatDuration(info.Duration)}",
        ];

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
}
