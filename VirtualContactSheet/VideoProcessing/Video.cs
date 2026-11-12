using SkiaSharp;

namespace VirtualContactSheet.VideoProcessing;

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
        int height = 0,
        bool evadeBlank = false,
        double blankThreshold = 0.08,
        IReadOnlyList<int>? alternatives = null,
        CancellationToken ct = default)
    {
        var info = await GetInfoAsync(ct).ConfigureAwait(false);
        var bitmap = await _capturer.CaptureAsync(Path, time, width, height, ct).ConfigureAwait(false);

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

            // A candidate that cannot be captured is just one fewer alternative.
            var candidate = await CaptureOrNoneAsync(alt, width, height, ct).ConfigureAwait(false);
            foreach (var frame in candidate.ToEnumerable())
            {
                if (FrameAnalysis.AverageBrightness(frame) >= blankThreshold)
                {
                    bitmap.Dispose();
                    return frame;
                }

                frame.Dispose();
            }
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

        // Resolve capture height: AspectRatio wins over ThumbnailHeight; 0 means ffmpeg auto-scales.
        int captureHeight;
        if (options.AspectRatio > 0)
        {
            captureHeight = (int)Math.Round(options.ThumbnailWidth / (double)options.AspectRatio);
            if (captureHeight % 2 != 0)
            {
                captureHeight++;
            }
        }
        else
        {
            captureHeight = options.ThumbnailHeight;
        }

        // Highlights first, then the regular grid.
        var frames = options.Highlights
            .Select(h => (Time: h, IsHighlight: true))
            .Concat(times.Select(t => (Time: t, IsHighlight: false)))
            .ToList();
        var captured = new List<(TimeIndex Time, bool IsHighlight, Option<SKBitmap> Bitmap)>(frames.Count);
        var thumbs = new List<ContactSheet.Thumbnail>(frames.Count);

        try
        {
            int done = 0;
            foreach (var (time, isHighlight) in frames)
            {
                ct.ThrowIfCancellationRequested();

                // A frame ffmpeg cannot capture (e.g. a corrupt stretch of the video) does not fail
                // the sheet; it gets a placeholder below, so a partly broken file still yields output.
                var bitmap = await CaptureFrameOrNoneAsync(time, options, captureHeight, ct).ConfigureAwait(false);
                captured.Add((time, isHighlight, bitmap));
                progress?.Report(++done / (double)frames.Count);
            }

            // The grid needs equally sized cells, and with auto height only a captured frame knows the size.
            var (width, height) = captured
                .WhereSelect(c => c.Bitmap)
                .FirstOrNone()
                .Select(b => (b.Width, b.Height))
                .GetOrElse(() => throw new CaptureException("None of the frames could be captured."));

            thumbs.AddRange(captured.Select(c => new ContactSheet.Thumbnail(
                c.Bitmap.GetOrElse(() => MissingFrame.Render(width, height)),
                c.Time.ToTimestamp(),
                c.IsHighlight)));

            var sheet = new ContactSheet(options)
            {
                HeaderOverride = VideoHeaderBuilder.Build(System.IO.Path.GetFileName(Path), info),
            };
            return sheet.Render(thumbs, options.Title);
        }
        finally
        {
            // Once built, thumbs holds every bitmap (captures and placeholders); before that, only captured does.
            if (thumbs.Count > 0)
            {
                foreach (var th in thumbs)
                {
                    th.Image.Dispose();
                }
            }
            else
            {
                foreach (var c in captured)
                {
                    c.Bitmap.AndThen(b => b.Dispose());
                }
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

    private async Task<Option<SKBitmap>> CaptureFrameOrNoneAsync(
        TimeIndex time,
        ContactSheetOptions options,
        int captureHeight,
        CancellationToken ct)
    {
        try
        {
            return await CaptureFrameAsync(
                time,
                options.ThumbnailWidth,
                captureHeight,
                options.BlankEvasion,
                options.BlankThreshold,
                options.BlankAlternatives,
                ct).ConfigureAwait(false);
        }
        catch (CaptureException)
        {
            return Option<SKBitmap>.None;
        }
    }

    private async Task<Option<SKBitmap>> CaptureOrNoneAsync(TimeIndex time, int width, int height, CancellationToken ct)
    {
        try
        {
            return await _capturer.CaptureAsync(Path, time, width, height, ct).ConfigureAwait(false);
        }
        catch (CaptureException)
        {
            return Option<SKBitmap>.None;
        }
    }
}
