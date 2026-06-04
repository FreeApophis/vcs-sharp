namespace VideoContactSheet;

/// <summary>Container-level metadata for a video file.</summary>
public sealed class VideoInfo
{
    public TimeSpan Duration { get; init; }

    public long? BitRate { get; init; }

    public long FileSize { get; init; }

    public string? FormatName { get; init; }

    public string Extension { get; init; } = string.Empty;

    public IReadOnlyList<VideoStream> VideoStreams { get; init; } = [];

    public IReadOnlyList<AudioStream> AudioStreams { get; init; } = [];

    /// <summary>First video stream, or null when there is none.</summary>
    public VideoStream? Video => VideoStreams.FirstOrDefault();

    /// <summary>First audio stream, or null when there is none.</summary>
    public AudioStream? Audio => AudioStreams.FirstOrDefault();
}
