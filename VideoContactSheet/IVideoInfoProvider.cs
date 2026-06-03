namespace VideoContactSheet;

/// <summary>Provides container-level metadata (duration, streams, size) for a video file.</summary>
public interface IVideoInfoProvider
{
    Task<VideoInfo> ProbeAsync(string path, CancellationToken ct = default);
}
