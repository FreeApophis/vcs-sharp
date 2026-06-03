using System.Globalization;
using System.Text.Json;

namespace VideoContactSheet;

/// <summary>Probes a video file's metadata using ffprobe (JSON output).</summary>
public sealed class FfprobeVideoInfoProvider
{
    private readonly string _ffprobePath;

    public FfprobeVideoInfoProvider(string ffprobePath = "ffprobe")
        => _ffprobePath = ffprobePath;

    public async Task<VideoInfo> ProbeAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Video file not found.", path);

        var args = new[]
        {
            "-v", "quiet",
            "-print_format", "json",
            "-show_format",
            "-show_streams",
            path,
        };

        var result = await ProcessRunner.RunAsync(_ffprobePath, args, ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            throw new CaptureException($"ffprobe failed (exit {result.ExitCode}): {result.StdErr}");

        using var doc = JsonDocument.Parse(result.StdOut);
        var root = doc.RootElement;

        var videoStreams = new List<VideoStream>();
        var audioStreams = new List<AudioStream>();

        if (root.TryGetProperty("streams", out var streams))
        {
            foreach (var s in streams.EnumerateArray())
            {
                var type = GetString(s, "codec_type");
                var index = GetInt(s, "index") ?? 0;

                if (type == "video")
                {
                    videoStreams.Add(new VideoStream
                    {
                        Index = index,
                        Codec = GetString(s, "codec_name"),
                        Width = GetInt(s, "width") ?? 0,
                        Height = GetInt(s, "height") ?? 0,
                        PixelFormat = GetString(s, "pix_fmt"),
                        BitRate = GetLong(s, "bit_rate"),
                        FrameRate = ParseRational(GetString(s, "avg_frame_rate"))
                                    ?? ParseRational(GetString(s, "r_frame_rate")),
                        DisplayAspectRatio = GetString(s, "display_aspect_ratio"),
                    });
                }
                else if (type == "audio")
                {
                    audioStreams.Add(new AudioStream
                    {
                        Index = index,
                        Codec = GetString(s, "codec_name"),
                        Channels = GetInt(s, "channels") ?? 0,
                        ChannelLayout = GetString(s, "channel_layout"),
                        SampleRate = GetInt(s, "sample_rate"),
                        BitRate = GetLong(s, "bit_rate"),
                    });
                }
            }
        }

        TimeSpan duration = default;
        long? bitRate = null;
        string? formatName = null;
        if (root.TryGetProperty("format", out var format))
        {
            var dur = GetString(format, "duration");
            if (dur != null && double.TryParse(dur, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                duration = TimeSpan.FromSeconds(seconds);
            bitRate = GetLong(format, "bit_rate");
            formatName = GetString(format, "format_name");
        }

        // Fall back to the video stream duration if the container has none.
        if (duration == default && videoStreams.Count > 0 && root.TryGetProperty("streams", out var sArr))
        {
            foreach (var s in sArr.EnumerateArray())
            {
                var dur = GetString(s, "duration");
                if (dur != null && double.TryParse(dur, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                {
                    duration = TimeSpan.FromSeconds(seconds);
                    break;
                }
            }
        }

        return new VideoInfo
        {
            Duration = duration,
            BitRate = bitRate,
            FileSize = new FileInfo(path).Length,
            FormatName = formatName,
            Extension = Path.GetExtension(path).TrimStart('.'),
            VideoStreams = videoStreams,
            AudioStreams = audioStreams,
        };
    }

    private static string? GetString(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int? GetInt(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.Number => v.GetInt32(),
            JsonValueKind.String when int.TryParse(v.GetString(), out var i) => i,
            _ => null,
        };
    }

    private static long? GetLong(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.Number => v.GetInt64(),
            JsonValueKind.String when long.TryParse(v.GetString(), out var i) => i,
            _ => null,
        };
    }

    private static double? ParseRational(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        var parts = value.Split('/');
        if (parts.Length == 2
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var num)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var den)
            && den != 0)
            return num / den;
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var single)
            ? single : null;
    }
}
