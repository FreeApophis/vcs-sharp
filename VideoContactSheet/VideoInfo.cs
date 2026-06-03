namespace VideoContactSheet;

/// <summary>Container-level metadata for a video file.</summary>
public sealed class VideoInfo
{
    public TimeSpan Duration { get; init; }

    public long? BitRate { get; init; }

    public long FileSize { get; init; }

    public string? FormatName { get; init; }

    public string Extension { get; init; } = string.Empty;

    public IReadOnlyList<VideoStream> VideoStreams { get; init; } = Array.Empty<VideoStream>();

    public IReadOnlyList<AudioStream> AudioStreams { get; init; } = Array.Empty<AudioStream>();

    /// <summary>First video stream, or null when there is none.</summary>
    public VideoStream? Video => VideoStreams.Count > 0 ? VideoStreams[0] : null;

    /// <summary>First audio stream, or null when there is none.</summary>
    public AudioStream? Audio => AudioStreams.Count > 0 ? AudioStreams[0] : null;
}

public sealed class VideoStream
{
    public int Index { get; init; }

    public string? Codec { get; init; }

    public string? Profile { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public string? PixelFormat { get; init; }

    public long? BitRate { get; init; }

    public double? FrameRate { get; init; }

    public string? DisplayAspectRatio { get; init; }

    /// <summary>Pixel aspect ratio derived from DAR/resolution; 1.0 for square pixels.</summary>
    public double AspectRatio
    {
        get
        {
            if (Width <= 0 || Height <= 0)
            {
                return 1.0;
            }

            if (!string.IsNullOrEmpty(DisplayAspectRatio) && DisplayAspectRatio.Contains(':'))
            {
                var parts = DisplayAspectRatio.Split(':');
                if (parts.Length == 2
                    && double.TryParse(parts[0], out var dw)
                    && double.TryParse(parts[1], out var dh)
                    && dh != 0)
                {
                    return (dw / dh) / ((double)Width / Height);
                }
            }

            return 1.0;
        }
    }
}

public sealed class AudioStream
{
    public int Index { get; init; }

    public string? Codec { get; init; }

    public int Channels { get; init; }

    public string? ChannelLayout { get; init; }

    public int? SampleRate { get; init; }

    public long? BitRate { get; init; }
}
