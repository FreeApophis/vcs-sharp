using FFMpegCore;

namespace VideoContactSheet;

/// <summary>Probes a video file's metadata using ffprobe via FFMpegCore.</summary>
public sealed class FfprobeVideoInfoProvider : IVideoInfoProvider
{
    private readonly FFOptions? _ffOptions;

    public FfprobeVideoInfoProvider(string? binaryFolder = null)
        => _ffOptions = binaryFolder is not null ? new FFOptions { BinaryFolder = binaryFolder } : null;

    public async Task<VideoInfo> ProbeAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Video file not found.", path);
        }

        IMediaAnalysis analysis;
        try
        {
            analysis = await FFProbe.AnalyseAsync(path, _ffOptions, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CaptureException($"ffprobe failed: {ex.Message}", ex);
        }

        var videoStreams = analysis.VideoStreams
            .Select(s => new VideoStream
            {
                Index = s.Index,
                Codec = s.CodecName,
                Width = s.Width,
                Height = s.Height,
                PixelFormat = s.PixelFormat,
                BitRate = s.BitRate,
                FrameRate = s.FrameRate,
                DisplayAspectRatio = s.DisplayAspectRatio.Width > 0
                    ? $"{s.DisplayAspectRatio.Width}:{s.DisplayAspectRatio.Height}"
                    : null,
            })
            .ToList();

        var audioStreams = analysis.AudioStreams
            .Select(s => new AudioStream
            {
                Index = s.Index,
                Codec = s.CodecName,
                Channels = s.Channels,
                ChannelLayout = s.ChannelLayout,
                SampleRate = s.SampleRateHz,
                BitRate = s.BitRate,
            })
            .ToList();

        return new VideoInfo
        {
            Duration = analysis.Duration,
            BitRate = (long?)analysis.Format.BitRate,
            FileSize = new FileInfo(path).Length,
            FormatName = analysis.Format.FormatName,
            Extension = Path.GetExtension(path).TrimStart('.'),
            VideoStreams = videoStreams,
            AudioStreams = audioStreams,
        };
    }
}
