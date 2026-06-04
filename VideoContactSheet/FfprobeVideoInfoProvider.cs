using FFMpegCore;

namespace VideoContactSheet;

/// <summary>Probes a video file's metadata using ffprobe via FFMpegCore.</summary>
public sealed class FfprobeVideoInfoProvider : IVideoInfoProvider
{
    private readonly FFOptions? _ffOptions;

    public FfprobeVideoInfoProvider(string? binaryFolder = null)
        => _ffOptions = binaryFolder is not null
            ? new FFOptions { BinaryFolder = binaryFolder }
            : null;

    public async Task<VideoInfo> ProbeAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Video file not found.", path);
        }

        try
        {
            IMediaAnalysis analysis = await FFProbe.AnalyseAsync(path, _ffOptions, cancellationToken).ConfigureAwait(false);

            return ToVideoInfo(path, analysis);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CaptureException($"ffprobe failed: {ex.Message}", ex);
        }
    }

    private static VideoInfo ToVideoInfo(string path, IMediaAnalysis analysis)
        => new()
        {
            Duration = analysis.Duration,
            BitRate = (long?)analysis.Format.BitRate,
            FileSize = new FileInfo(path).Length,
            FormatName = analysis.Format.FormatName,
            Extension = Path.GetExtension(path).TrimStart('.'),
            VideoStreams = [.. analysis.VideoStreams.Select(ToVideoStream)],
            AudioStreams = [.. analysis.AudioStreams.Select(ToAudioStream)],
        };

    private static AudioStream ToAudioStream(FFMpegCore.AudioStream s)
        => new()
        {
            Index = s.Index,
            Codec = s.CodecName,
            Channels = s.Channels,
            ChannelLayout = s.ChannelLayout,
            SampleRate = s.SampleRateHz,
            BitRate = s.BitRate,
        };

    private static VideoStream ToVideoStream(FFMpegCore.VideoStream s)
        => new()
        {
            Index = s.Index,
            Codec = s.CodecName,
            Profile = s.Profile,
            Width = s.Width,
            Height = s.Height,
            PixelFormat = s.PixelFormat,
            BitRate = s.BitRate,
            FrameRate = s.FrameRate,
            DisplayAspectRatio = s.DisplayAspectRatio.Width > 0
                            ? $"{s.DisplayAspectRatio.Width}:{s.DisplayAspectRatio.Height}"
                            : null,
        };
}
