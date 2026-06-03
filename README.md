# VideoContactSheet (.NET)

A C# port of [vcs.rb](https://github.com/FreeApophis/vcs.rb) — a **Video Contact Sheet** generator.
It extracts frames from a video at regular intervals and composes them into a grid ("contact sheet")
with a metadata header, optional title, per-thumbnail timestamps, drop shadows, polaroid frames,
and a signature footer.

- **Frame capture & metadata**: `ffmpeg` / `ffprobe` (must be installed and on `PATH`)
- **Composition / drawing**: [SkiaSharp](https://github.com/mono/SkiaSharp) (MIT) — chosen for its
  native drop-shadow, text, and canvas compositing support
- **Targets**: .NET 8, cross-platform (Windows / Linux / macOS)

## Requirements

- .NET 8 SDK
- `ffmpeg` and `ffprobe` available on the system `PATH`
- On headless Linux, you may also need `libfontconfig1` for text rendering

## Library usage

```csharp
using VideoContactSheet;

var video = new Video("movie.mkv");

// Metadata
var info = await video.GetInfoAsync();
Console.WriteLine($"Duration: {info.Duration}, {info.Video?.Width}x{info.Video?.Height}");

// Build a 3x3 sheet
var options = new ContactSheetOptions
{
    Columns = 3,
    Rows = 3,
    ThumbnailWidth = 320,
    Format = SheetFormat.Jpg,
    Title = "My Movie",
    SoftShadow = true,
    Timestamp = true,
};
await video.SaveContactSheetAsync("out.jpg", options);

// Single frame
var bmp = await video.CaptureFrameAsync(TimeIndex.Parse("1:22"), width: 640, evadeBlank: true);
```

## CLI

The `vcs` CLI mirrors the original script's options:

```
vcs video.avi
vcs -i 3m30 input.wmv -o output.jpg
vcs --from 3m --to 18m -i 2m input.avi
vcs -c 4 -r 5 --polaroid --no-shadow -T "Holiday" clip.mp4
```

Run `vcs --help` for the full list.

## Feature mapping vs. vcs.rb

| vcs.rb feature            | Status | Notes                                            |
| ------------------------- | :----: | ------------------------------------------------ |
| Grid (rows × columns)     |   ✅   | `Columns`, `Rows`                                |
| Interval-based capture    |   ✅   | `Interval`                                       |
| From / To range           |   ✅   | `From`, `To`                                     |
| Thumbnail width           |   ✅   | `ThumbnailWidth`                                 |
| Formats png/jpg           |   ✅   | + webp                                           |
| Title / header / signature|   ✅   | metadata header auto-built from ffprobe          |
| Timestamp overlay         |   ✅   | `Timestamp`                                      |
| Drop shadow               |   ✅   | `SoftShadow`, `ShadowSize`                       |
| Polaroid frame            |   ✅   | `Polaroid`                                       |
| Highlights                |   ✅   | `Highlights` (rendered in a band on top)         |
| Blank-frame evasion       |   ✅   | `BlankEvasion`, `BlankThreshold`, alternatives   |
| Single-frame capture      |   ✅   | `CaptureFrameAsync`                              |
| Video metadata (streams)  |   ✅   | `GetInfoAsync` → `VideoInfo`                      |
| Capturer: ffmpeg          |   ✅   | `FfmpegCapturer` (implement `IFrameCapturer` for libav/mplayer) |
| YAML profiles             |   ⬜   | configure via `ContactSheetOptions` in code instead |

## Architecture

```
Video                     orchestrator (probe → capture → compose)
 ├─ FfprobeVideoInfoProvider   ffprobe JSON → VideoInfo
 ├─ IFrameCapturer             frame extraction abstraction
 │   └─ FfmpegCapturer         ffmpeg → PNG → SKBitmap
 ├─ TimeIndex                  flexible time parsing ("3m30", "1:22", "90")
 └─ ContactSheet               SkiaSharp grid composition + styling
     └─ ContactSheetOptions    all grid/style/filter settings
```

Swap in another capturer (libav, mplayer) by implementing `IFrameCapturer` and passing it to the
`Video` constructor.

## License

Mirror of a GPL-3.0 project; treat this port accordingly.
