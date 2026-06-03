namespace VideoContactSheet.Test;

/// <summary>Deterministic, ffmpeg-free building blocks for the rendering and pipeline tests.</summary>
internal static class TestFrames
{
    /// <summary>A fixed palette so tests can assert exact pixel colours.</summary>
    public static readonly SKColor[] Palette =
    [
        new(0xE0, 0x10, 0x10),
        new(0x10, 0xE0, 0x10),
        new(0x10, 0x10, 0xE0),
        new(0xE0, 0xE0, 0x10),
        new(0xE0, 0x10, 0xE0),
        new(0x10, 0xE0, 0xE0),
        new(0xF0, 0x80, 0x10),
        new(0x80, 0x10, 0xF0),
    ];

    public static SKBitmap Solid(int width, int height, SKColor color)
    {
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        return bitmap;
    }

    /// <summary>Builds <paramref name="count"/> solid thumbnails, cycling through <see cref="Palette"/>.</summary>
    public static List<ContactSheet.Thumbnail> Grid(int count, int width = 160, int height = 90)
    {
        var thumbnails = new List<ContactSheet.Thumbnail>(count);
        for (int i = 0; i < count; i++)
        {
            thumbnails.Add(new ContactSheet.Thumbnail(Solid(width, height, Palette[i % Palette.Length]), new TimeIndex(i * 10)));
        }

        return thumbnails;
    }

    public static void DisposeAll(IEnumerable<ContactSheet.Thumbnail> thumbnails)
    {
        foreach (var thumbnail in thumbnails)
        {
            thumbnail.Image.Dispose();
        }
    }
}

/// <summary>An <see cref="IFrameCapturer"/> that returns solid colour frames without invoking ffmpeg.</summary>
internal sealed class FakeCapturer : IFrameCapturer
{
    public int CaptureCount { get; private set; }

    public Task<SKBitmap> CaptureAsync(string videoPath, TimeIndex time, int width, CancellationToken ct = default)
    {
        CaptureCount++;

        // Colour derived deterministically from the requested time so frames are distinguishable.
        int seconds = (int)time.TotalSeconds;
        var color = new SKColor((byte)((seconds * 7) % 256), (byte)((seconds * 13) % 256), (byte)((seconds * 29) % 256));
        int height = (width * 9) / 16;
        return Task.FromResult(TestFrames.Solid(width, height, color));
    }
}

/// <summary>An <see cref="IVideoInfoProvider"/> returning fixed metadata without invoking ffprobe.</summary>
internal sealed class FakeProbe : IVideoInfoProvider
{
    private readonly VideoInfo _info;

    public FakeProbe(TimeSpan? duration = null)
        => _info = new VideoInfo
        {
            Duration = duration ?? TimeSpan.FromSeconds(100),
            FileSize = 1_000_000,
            FormatName = "fake",
            Extension = "mkv",
            VideoStreams =
            [
                new VideoStream { Index = 0, Codec = "h264", Width = 1920, Height = 1080, FrameRate = 25 },
            ],
            AudioStreams = Array.Empty<AudioStream>(),
        };

    public Task<VideoInfo> ProbeAsync(string path, CancellationToken ct = default) => Task.FromResult(_info);
}
