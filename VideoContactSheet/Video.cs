using SkiaSharp;

namespace VideoContactSheet;

/// <summary>
/// Top-level entry point. Wraps a video file and produces contact sheets or single frames.
/// </summary>
public sealed class Video
{
    private readonly IFrameCapturer _capturer;

    private readonly IVideoInfoProvider _probe;

    private VideoInfo? _info;

    public string Path { get; }

    public Video(
        string path,
        IFrameCapturer? capturer = null,
        IVideoInfoProvider? probe = null,
        string? ffBinaryFolder = null)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        _capturer = capturer ?? new FfmpegCapturer(ffBinaryFolder);
        _probe = probe ?? new FfprobeVideoInfoProvider(ffBinaryFolder);
    }

    /// <summary>Probe and cache the video's metadata.</summary>
    public async Task<VideoInfo> GetInfoAsync(CancellationToken ct = default)
        => _info ??= await _probe.ProbeAsync(Path, ct).ConfigureAwait(false);

    public async Task<bool> IsValidAsync(CancellationToken ct = default)
    {
        try
        {
            var info = await GetInfoAsync(ct).ConfigureAwait(false);
            return info.Duration > TimeSpan.Zero && info.VideoStreams.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Capture a single frame, with optional blank-frame evasion.</summary>
    public async Task<SKBitmap> CaptureFrameAsync(
        TimeIndex time,
        int width,
        bool evadeBlank = false,
        double blankThreshold = 0.08,
        IReadOnlyList<int>? alternatives = null,
        CancellationToken ct = default)
    {
        var info = await GetInfoAsync(ct).ConfigureAwait(false);
        var bitmap = await _capturer.CaptureAsync(Path, time, width, ct).ConfigureAwait(false);

        if (!evadeBlank || FrameAnalysis.AverageBrightness(bitmap) >= blankThreshold)
        {
            return bitmap;
        }

        // Try nearby offsets to dodge a blank/black frame.
        foreach (var offset in alternatives ?? [-5, 5, -10, 10, -30, 30])
        {
            ct.ThrowIfCancellationRequested();
            var alt = new TimeIndex(time.TotalSeconds + offset);
            if (alt.TotalSeconds < 0 || alt.Value > info.Duration)
            {
                continue;
            }

            var candidate = await _capturer.CaptureAsync(Path, alt, width, ct).ConfigureAwait(false);
            if (FrameAnalysis.AverageBrightness(candidate) >= blankThreshold)
            {
                bitmap.Dispose();
                return candidate;
            }

            candidate.Dispose();
        }

        return bitmap;
    }

    /// <summary>Compute the time indices for the grid based on options.</summary>
    public IReadOnlyList<TimeIndex> ComputeTimes(VideoInfo info, ContactSheetOptions options)
    {
        var from = options.From?.Value ?? TimeSpan.Zero;
        var to = options.To?.Value ?? info.Duration;
        if (to <= from)
        {
            to = info.Duration;
        }

        var span = to - from;
        var times = new List<TimeIndex>();

        if (options.Interval is { } interval && interval.TotalSeconds > 0)
        {
            for (var t = from; t < to; t += interval.Value)
            {
                times.Add(new TimeIndex(t));
            }
        }
        else
        {
            int count = Math.Max(1, options.Columns * options.Rows);

            // Evenly distribute, sampling at the middle of each segment.
            for (int i = 0; i < count; i++)
            {
                double fraction = (i + 0.5) / count;
                times.Add(new TimeIndex(from + TimeSpan.FromSeconds(span.TotalSeconds * fraction)));
            }
        }

        return times;
    }

    /// <summary>Build the contact sheet and return encoded image bytes.</summary>
    public async Task<byte[]> BuildContactSheetAsync(
        ContactSheetOptions options,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var info = await GetInfoAsync(ct).ConfigureAwait(false);
        if (info.VideoStreams.Count == 0)
        {
            throw new CaptureException("No video stream found.");
        }

        var times = ComputeTimes(info, options);
        var thumbs = new List<ContactSheet.Thumbnail>();

        // Highlights first.
        int total = options.Highlights.Count + times.Count;
        int done = 0;

        foreach (var h in options.Highlights)
        {
            ct.ThrowIfCancellationRequested();
            var bmp = await CaptureFrameAsync(
                h,
                options.ThumbnailWidth,
                options.BlankEvasion,
                options.BlankThreshold,
                options.BlankAlternatives,
                ct).ConfigureAwait(false);
            thumbs.Add(new ContactSheet.Thumbnail(bmp, h, IsHighlight: true));
            progress?.Report(++done / (double)total);
        }

        foreach (var t in times)
        {
            ct.ThrowIfCancellationRequested();
            var bmp = await CaptureFrameAsync(
                t,
                options.ThumbnailWidth,
                options.BlankEvasion,
                options.BlankThreshold,
                options.BlankAlternatives,
                ct).ConfigureAwait(false);
            thumbs.Add(new ContactSheet.Thumbnail(bmp, t));
            progress?.Report(++done / (double)total);
        }

        try
        {
            var sheet = new ContactSheet(options)
            {
                HeaderLinesOverride = BuildHeaderLines(info, options),
            };
            return sheet.Render(thumbs, options.Title);
        }
        finally
        {
            foreach (var th in thumbs)
            {
                th.Image.Dispose();
            }
        }
    }

    /// <summary>Build a contact sheet and write it to disk.</summary>
    public async Task SaveContactSheetAsync(
        string outputPath,
        ContactSheetOptions options,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var bytes = await BuildContactSheetAsync(options, progress, ct).ConfigureAwait(false);
        await File.WriteAllBytesAsync(outputPath, bytes, ct).ConfigureAwait(false);
    }

    private string[] BuildHeaderLines(VideoInfo info, ContactSheetOptions options)
    {
        if (!options.ShowHeader)
        {
            return [];
        }

        var v = info.Video;
        var a = info.Audio;
        var lines = new List<string>
        {
            $"File: {System.IO.Path.GetFileName(Path)}  ({FormatBytes(info.FileSize)})",
            $"Duration: {FormatDuration(info.Duration)}" +
                (v != null ? $"   Resolution: {v.Width}x{v.Height}" : string.Empty) +
                (v?.FrameRate is { } fps ? $"   {fps:0.##} fps" : string.Empty),
        };

        var codecLine = string.Empty;
        if (v?.Codec != null)
        {
            codecLine += $"Video: {v.Codec}";
        }

        if (a?.Codec != null)
        {
            codecLine += (codecLine.Length > 0 ? "   " : string.Empty) + $"Audio: {a.Codec}" +
                (a.SampleRate is { } sr ? $" {sr} Hz" : string.Empty) +
                (a.Channels > 0 ? $" {a.Channels}ch" : string.Empty);
        }

        if (codecLine.Length > 0)
        {
            lines.Add(codecLine);
        }

        return lines.ToArray();
    }

    private static string FormatDuration(TimeSpan d)
        => d.TotalHours >= 1
            ? $"{(int)d.TotalHours}:{d.Minutes:D2}:{d.Seconds:D2}"
            : $"{d.Minutes:D2}:{d.Seconds:D2}";

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
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
