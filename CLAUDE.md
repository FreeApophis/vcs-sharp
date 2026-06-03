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
- `FfmpegCapturer.cs` — frame extraction via ffmpeg; also `IFrameCapturer` interface and `FrameAnalysis`
- `FfprobeVideoInfoProvider.cs` — probes video metadata via ffprobe JSON output → `VideoInfo`
- `TimeIndex.cs` — flexible time parser ("3m30", "1:22", "90", "1h2m3s")
- `ProcessRunner.cs` — async process wrapper; also `ToolNotFoundException` / `CaptureException`

## Target framework

`Directory.Build.props` sets `TargetFramework` to `net10.0` for all projects.

## Runtime dependencies

`ffmpeg` and `ffprobe` must be on `PATH` at runtime. The library accepts custom paths via constructors if they are not on PATH.

## Known SkiaSharp API quirk

`SKFont.MeasureText(string)` is not available in SkiaSharp 2.88.x when targeting .NET 10 — only the `ReadOnlySpan<ushort>` overload is exposed. The workaround is in `ContactSheet.cs`:

```csharp
private static float MeasureTextWidth(SKFont font, string text)
    => font.MeasureText(MemoryMarshal.Cast<char, ushort>(text.AsSpan()));
```

All `font.MeasureText(...)` calls in `ContactSheet.cs` use this helper.

## Package management

Uses Central Package Management (`Directory.Packages.props`). Add new packages there with `<PackageVersion>` before referencing them in individual projects.

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
  -q, --quiet
      --continue  Keep going on per-file errors
```
