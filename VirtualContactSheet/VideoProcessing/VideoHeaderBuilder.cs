namespace VirtualContactSheet.VideoProcessing;

/// <summary>
/// Builds the two-column metadata header, mirroring the original vcs.rb layout:
/// left column = filename / file size / length, right column = dimensions / format / fps.
/// </summary>
public static class VideoHeaderBuilder
{
    public static HeaderColumns Build(string fileName, VideoInfo info)
        => new(LeftColumn(fileName, info), RightColumn(info));

    private static List<string> RightColumn(VideoInfo info)
    {
        var videoInfo = info.Video;
        var audioInfo = info.Audio;

        var right = new List<string>();
        if (videoInfo is not null)
        {
            Dimensions(videoInfo).AndThen(right.Add);
        }

        var format = FormatCodecs(videoInfo, audioInfo);
        if (format is not null)
        {
            right.Add($"Format: {format}");
        }

        if (videoInfo?.FrameRate is { } fps)
        {
            right.Add($"FPS: {fps:0.##}");
        }

        return right;
    }

    private static Option<string> Dimensions(VideoStream video)
        => from width in video.Width
           from height in video.Height
           select $"Dimensions: {width}x{height}";

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
