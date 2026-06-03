# VideoContactSheet

A C# port of [vcs.rb](https://github.com/FreeApophis/vcs.rb) — generates contact sheets (frame grids) from video files using ffmpeg/ffprobe for capture and SkiaSharp for composition.

## Build

```
dotnet build
dotnet test
dotnet run --project VideoContactSheet.Cli -- <video> [options]
```

The CLI binary is named `vcs`.

## Project structure

| Project | Purpose |
|---|---|
| `VideoContactSheet/` | Core library (NuGet-packageable) |
| `VideoContactSheet.Cli/` | CLI front-end (`vcs` binary) |
| `VideoContactSheet.Test/` | xUnit test project |

Key source files in the library:

- `Video.cs` — top-level orchestrator: probe → compute times → capture → compose
- `ContactSheet.cs` — SkiaSharp grid layout and rendering
- `ContactSheetOptions.cs` — all grid/style settings (`Columns`, `Rows`, `Interval`, `Polaroid`, `SoftShadow`, …)
- `FfmpegCapturer.cs` — `IFrameCapturer` interface, `FfmpegCapturer` (FFMpegCore pipe → PNG → SKBitmap), `FrameAnalysis`
- `FfprobeVideoInfoProvider.cs` — `FFProbe.AnalyseAsync` → `VideoInfo`
- `TimeIndex.cs` — flexible time parser ("3m30", "1:22", "90", "1h2m3s")
- `ProcessRunner.cs` — public exception types only: `ToolNotFoundException`, `CaptureException`

## Target framework

`Directory.Build.props` sets `TargetFramework` to `net10.0` for all projects.

## Runtime dependencies / bundled ffmpeg

`ffmpeg 8.1.1` binaries (Windows x64) live in `tools/ffmpeg/win-x64/` and are copied to the CLI build output by `VideoContactSheet.Cli.csproj` via a Windows-conditional `<Content>` rule. The CLI auto-detects them on startup, so no global PATH install is needed.

The binaries are git-ignored (~97 MB each). Other developers or CI reproduce them with:
```
pwsh tools/download-ffmpeg.ps1
```

To use bundled binaries from the library:
```csharp
var video = new Video("movie.mkv", ffBinaryFolder: AppContext.BaseDirectory);
```

To add Linux/macOS support: download the respective platform builds, put them in `tools/ffmpeg/linux-x64/` (etc.), and add matching `<Content>` blocks with an `IsOSPlatform('Linux')` condition in the CLI `.csproj`.

## Known SkiaSharp API quirk

`SKFont.MeasureText(string)` is not available in SkiaSharp 2.88.x when targeting .NET 10 — only the `ReadOnlySpan<ushort>` overload is exposed. The workaround is in `ContactSheet.cs`:

```csharp
private static float MeasureTextWidth(SKFont font, string text)
    => font.MeasureText(MemoryMarshal.Cast<char, ushort>(text.AsSpan()));
```

All `font.MeasureText(...)` calls in `ContactSheet.cs` use this helper.

## Package management

Uses Central Package Management (`Directory.Packages.props`). Add new packages there with `<PackageVersion>` before referencing them in individual projects.

Key library dependencies: `FFMpegCore 5.4.0`, `SkiaSharp 2.88.8`.

## CLI options

```
vcs [options] <video> [<video> ...]

  -i, --interval  Capture interval (e.g. 3m30, 90, 1:22)
  -c, --columns   Grid columns (default 4)
  -r, --rows      Grid rows (default 4)
  -W, --width     Thumbnail width px (default 320)
      --from      Start time
  -t, --to        End time
  -f, --format    png | jpg | webp
  -T, --title     Sheet title
  -o, --output    Output file
  -l, --highlight Extra highlight frame at TIME (repeatable)
      --[no-]timestamp  (default on)
      --[no-]polaroid   (default off)
      --[no-]shadow     (default on)
      --ffmpeg-folder  Folder containing ffmpeg/ffprobe binaries
  -q, --quiet
      --continue  Keep going on per-file errors
```
