using SkiaSharp;

namespace VideoContactSheet;

/// <summary>Extracts single frames from a video at a given time index.</summary>
public interface IFrameCapturer
{
    Task<SKBitmap> CaptureAsync(string videoPath, TimeIndex time, int width, CancellationToken ct = default);
}

/// <summary>Frame capturer backed by ffmpeg. Renders one frame to PNG on stdout and decodes it.</summary>
public sealed class FfmpegCapturer : IFrameCapturer
{
    private readonly string _ffmpegPath;

    public FfmpegCapturer(string ffmpegPath = "ffmpeg") => _ffmpegPath = ffmpegPath;

    public async Task<SKBitmap> CaptureAsync(string videoPath, TimeIndex time, int width, CancellationToken ct = default)
    {
        // -ss before -i is fast (keyframe) seeking, accurate enough for thumbnails.
        var args = new List<string>
        {
            "-y",
            "-ss", time.TotalSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
            "-i", videoPath,
            "-frames:v", "1",
        };
        if (width > 0)
            args.AddRange(new[] { "-vf", $"scale={width}:-1" });
        args.AddRange(new[] { "-f", "image2pipe", "-vcodec", "png", "pipe:1" });

        var bytes = await RunCaptureAsync(args, ct).ConfigureAwait(false);
        var bitmap = SKBitmap.Decode(bytes)
            ?? throw new CaptureException($"Failed to decode frame at {time}.");
        return bitmap;
    }

    private async Task<byte[]> RunCaptureAsync(IEnumerable<string> args, CancellationToken ct)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = _ffmpegPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var process = new System.Diagnostics.Process { StartInfo = psi };
        try { process.Start(); }
        catch (Exception ex)
        {
            throw new ToolNotFoundException(
                $"Could not start '{_ffmpegPath}'. Make sure ffmpeg is installed and on PATH.", ex);
        }

        using var ms = new MemoryStream();
        var copyTask = process.StandardOutput.BaseStream.CopyToAsync(ms, ct);
        var errTask = process.StandardError.ReadToEndAsync(ct);

        await Task.WhenAll(copyTask, process.WaitForExitAsync(ct)).ConfigureAwait(false);
        var err = await errTask.ConfigureAwait(false);

        if (process.ExitCode != 0 || ms.Length == 0)
            throw new CaptureException($"ffmpeg capture failed (exit {process.ExitCode}): {err}");

        return ms.ToArray();
    }
}

/// <summary>Utility to measure average brightness, used for blank-frame evasion.</summary>
public static class FrameAnalysis
{
    /// <summary>Mean luma in the 0..1 range.</summary>
    public static double AverageBrightness(SKBitmap bitmap)
    {
        // Sample on a grid for speed instead of every pixel.
        const int step = 8;
        double sum = 0;
        int count = 0;
        for (int y = 0; y < bitmap.Height; y += step)
        {
            for (int x = 0; x < bitmap.Width; x += step)
            {
                var c = bitmap.GetPixel(x, y);
                sum += (0.2126 * c.Red + 0.7152 * c.Green + 0.0722 * c.Blue) / 255.0;
                count++;
            }
        }
        return count == 0 ? 0 : sum / count;
    }
}
