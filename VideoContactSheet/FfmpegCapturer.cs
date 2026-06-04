using FFMpegCore;
using FFMpegCore.Pipes;
using SkiaSharp;

namespace VideoContactSheet;

/// <summary>Frame capturer backed by FFMpegCore. Pipes one PNG frame to memory and decodes it with SkiaSharp.</summary>
public sealed class FfmpegCapturer : IFrameCapturer
{
    private readonly FFOptions? _ffOptions;

    public FfmpegCapturer(string? binaryFolder = null)
        => _ffOptions = binaryFolder is not null ? new FFOptions { BinaryFolder = binaryFolder } : null;

    public async Task<SKBitmap> CaptureAsync(string videoPath, TimeIndex time, int width, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();

        var processor = FFMpegArguments
            .FromFileInput(videoPath, false, input => input.Seek(time.Value))
            .OutputToPipe(new StreamPipeSink(ms), output =>
            {
                if (width > 0)
                {
                    output.WithCustomArgument($"-vf scale={width}:-1");
                }

                output.WithVideoCodec("png")
                      .WithFrameOutputCount(1)
                      .ForceFormat("image2pipe");
            });

        try
        {
            var task = _ffOptions is not null
                ? processor.ProcessAsynchronously(true, _ffOptions)
                : processor.ProcessAsynchronously();
            await task.WaitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CaptureException($"ffmpeg capture failed at {time}: {ex.Message}", ex);
        }

        if (ms.Length == 0)
        {
            throw new CaptureException($"ffmpeg returned no data for frame at {time}.");
        }

        ms.Position = 0;
        return SKBitmap.Decode(ms)
            ?? throw new CaptureException($"Failed to decode captured frame at {time}.");
    }
}
