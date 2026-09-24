# VirtualContactSheet (.NET)

[![NuGet](https://img.shields.io/nuget/v/VirtualContactSheet.svg)](https://www.nuget.org/packages/VirtualContactSheet)
[![CI](https://github.com/FreeApophis/vcs-sharp/actions/workflows/ci.yml/badge.svg)](https://github.com/FreeApophis/vcs-sharp/actions/workflows/ci.yml)

**Virtual Contact Sheet** — a contact-sheet generator, originally a C# port of
[vcs.rb](https://github.com/FreeApophis/vcs.rb). It composes a grid ("contact sheet") with a
metadata header, optional title, per-thumbnail captions, drop shadows, polaroid frames, and a
signature footer — either from frames extracted from a **video** at regular intervals, or from a
folder (or list) of **images**.

- **Frame capture & metadata**: [FFMpegCore](https://github.com/rosenbjerg/FFMpegCore) — wraps
  `ffmpeg` / `ffprobe`; binaries can be on `PATH` or pointed to via `ffBinaryFolder`
- **Composition / drawing**: [SkiaSharp](https://github.com/mono/SkiaSharp) (MIT) — chosen for its
  native drop-shadow, text, and canvas compositing support
- **Image decoding**: SkiaSharp again — jpg, png, gif, bmp, webp, heif/heic, avif …, with EXIF
  orientation applied
- **Targets**: .NET 10, cross-platform (Windows / Linux / macOS)

## Example

A 4×4 contact sheet with a custom footer:

![Example contact sheet](https://raw.githubusercontent.com/FreeApophis/vcs-sharp/main/docs/example-contact-sheet.png)

```csharp
using VirtualContactSheet;
using VirtualContactSheet.VideoProcessing;

var video = new Video("ons3on3cup_hdtv.mp4");

var options = new ContactSheetOptions
{
    Columns = 4,
    Rows = 4,
    Signature = "Made in .NET with VirtualContactSheet",
};

await video.SaveContactSheetAsync("example-contact-sheet.png", options);
```

Or with the CLI:

```sh
vcs ons3on3cup_hdtv.mp4 -c 4 -r 4 -s "Made in .NET with VirtualContactSheet" -o example-contact-sheet.png
```

The same thing from a folder of photos, no ffmpeg involved:

```csharp
using VirtualContactSheet;
using VirtualContactSheet.ImageProcessing;

var photos = ImageCollection.FromFolder("holiday-2026");

await photos.SaveContactSheetAsync("contact-sheet.png", new ContactSheetOptions
{
    Columns = 4,
    Rows = 4,
    Title = "Holiday 2026",
});
```

## Requirements

- .NET 10 SDK
- `ffmpeg` and `ffprobe` binaries — only for video; image sheets need neither. The CLI project bundles Windows x64 binaries in its build output automatically (run `tools/download-ffmpeg.ps1` first if they are missing). Alternatively supply `ffBinaryFolder` or put them on `PATH`.
- On headless Linux, you may also need `libfontconfig1` for text rendering

## Namespaces

| Namespace | Contents |
| --------- | -------- |
| `VirtualContactSheet` | Everything shared: `ContactSheet`, `ContactSheetOptions`, `TextStyle`, `SheetFormat`, `HeaderColumns`, `TimeIndex`, `CaptureException` |
| `VirtualContactSheet.VideoProcessing` | `Video`, `VideoInfo`, `IFrameCapturer` / `FfmpegCapturer`, `IVideoInfoProvider` / `FfprobeVideoInfoProvider`, `FrameAnalysis` |
| `VirtualContactSheet.ImageProcessing` | `ImageCollection`, `ImageInfo`, `IImageLoader` / `SkiaImageLoader`, `IImageInfoProvider` / `SkiaImageInfoProvider`, `ImageFit`, `ImageSelection` |

## Library usage — video

```csharp
using VirtualContactSheet;
using VirtualContactSheet.VideoProcessing;

// ffmpeg/ffprobe on PATH:
var video = new Video("movie.mkv");

// — or — binaries shipped next to the exe:
var video = new Video("movie.mkv", ffBinaryFolder: AppContext.BaseDirectory);

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

## Library usage — images

`ImageCollection` mirrors `Video`: point it at a source, reuse the same `ContactSheetOptions`, and
call the same `SaveContactSheetAsync`. No ffmpeg involved.

```csharp
using VirtualContactSheet;
using VirtualContactSheet.ImageProcessing;

// Every supported image in a folder, ordered by file name:
var photos = ImageCollection.FromFolder("holiday-2026", recursive: true);

// — or — an explicit list, kept in the order given:
var photos = ImageCollection.FromFiles(["cover.jpg", "beach.png", "sunset.jpg"]);

// Metadata (dimensions/format/size per image, read from the file headers)
var info = await photos.GetInfoAsync();
Console.WriteLine($"{info.Count} images, {info.First?.Width}x{info.First?.Height}");

var options = new ContactSheetOptions
{
    Columns = 4,
    Rows = 4,
    ThumbnailWidth = 320,
    Title = "Holiday 2026",
    Format = SheetFormat.Jpg,
};

await photos.SaveContactSheetAsync("holiday.jpg", options);
```

### How the options map to images

| Option | Meaning for an image collection |
| ------ | ------------------------------- |
| `Columns`, `Rows` | Grid capacity. More images than cells: see `Selection` below |
| `ThumbnailWidth` | Cell width; the height follows `AspectRatio`, then `ThumbnailHeight`, else the aspect ratio of the first image |
| `Timestamp` | Toggles the caption overlay — the file name instead of a time index |
| `Title`, `Signature`, `ShowHeader` | As for video; the header lists folder, image count, total size, dimensions and formats |
| `Interval`, `From`, `To`, `Highlights`, blank-frame evasion | Video-only, ignored here |

Extra knobs that live on the collection itself:

```csharp
// More images than cells? Sample evenly (default) or render all of them.
photos.Selection = ImageSelection.All;

// Caption text per thumbnail; return null for none.
photos.Caption = image => Path.GetFileNameWithoutExtension(image.Path);

// Cells are uniform, so images that do not match are letterboxed (default),
// cropped, or stretched:
var cropped = ImageCollection.FromFolder("photos", loader: new SkiaImageLoader(ImageFit.Cover));
```

Implement `IImageLoader` (or `IImageInfoProvider`) to plug in a different decoder, exactly like
`IFrameCapturer` for video.

## CLI

Install as a .NET global tool (requires `ffmpeg`/`ffprobe` on `PATH`):

```sh
dotnet tool install -g VirtualContactSheet.Cli
```

The `vcs` CLI mirrors the original script's options:

```
vcs video.avi
vcs -i 3m30 input.wmv -o output.jpg
vcs --from 3m --to 18m -i 2m input.avi
vcs -c 4 -r 5 --polaroid --no-shadow -T "Holiday" clip.mp4
```

Run `vcs --help` for the full list. Use `--ffmpeg-folder <dir>` to point at a local copy of the binaries instead of relying on `PATH`.

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
| Capturer: ffmpeg          |   ✅   | `FfmpegCapturer` via FFMpegCore (implement `IFrameCapturer` for libav/mplayer) |
| YAML profiles             |   ⬜   | configure via `ContactSheetOptions` in code instead |

## Architecture

```
ContactSheet                  SkiaSharp grid composition + styling   (shared)
 └─ ContactSheetOptions       all grid/style/filter settings

VideoProcessing
 Video                        orchestrator (probe → capture → compose)
 ├─ IVideoInfoProvider        metadata abstraction
 │   └─ FfprobeVideoInfoProvider   FFProbe.AnalyseAsync → VideoInfo
 ├─ IFrameCapturer            frame extraction abstraction
 │   └─ FfmpegCapturer        FFMpegCore pipe → PNG → SKBitmap
 └─ TimeIndex                 flexible time parsing ("3m30", "1:22", "90")

ImageProcessing
 ImageCollection              orchestrator (probe → load/scale → compose)
 ├─ IImageInfoProvider        metadata abstraction
 │   └─ SkiaImageInfoProvider SKCodec header → ImageInfo
 ├─ IImageLoader              decode/scale abstraction
 │   └─ SkiaImageLoader       decode → EXIF straighten → fit the cell (contain/cover/stretch)
 └─ ImageSelection            how a large collection is reduced to the grid
```

Swap in another capturer (libav, mplayer) by implementing `IFrameCapturer` and passing it to the
`Video` constructor; the same goes for `IImageLoader` and `ImageCollection`.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## License

Mirror of a GPL-3.0 project; treat this port accordingly.
