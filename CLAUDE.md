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

In SkiaSharp 2.88.x on .NET 10, the string/char-span/encoding overloads of `SKFont.MeasureText`
and `SKFont.GetGlyphs` are not exposed — only the glyph-ID (`ReadOnlySpan<ushort>`) and
codepoint (`ReadOnlySpan<int>`) ones are. So to measure text width you must first map the text
to **real glyph IDs** via its Unicode codepoints, then measure those. The helper in
`ContactSheet.cs`:

```csharp
private static float MeasureTextWidth(SKFont font, string text)
{
    // ... build int[] codepoints from text (surrogate-aware) ...
    var glyphs = new ushort[codepoints.Count];
    font.GetGlyphs(codepoints.ToArray(), glyphs);   // (ReadOnlySpan<int>, Span<ushort>)
    return font.MeasureText(glyphs);                // (ReadOnlySpan<ushort>)
}
```

All `font.MeasureText(...)` calls in `ContactSheet.cs` use this helper.

> ⚠️ Do **not** `MemoryMarshal.Cast<char, ushort>` the string and pass it to `MeasureText`:
> that overload treats the values as glyph IDs, but char codes are not glyph IDs, so it
> measures unrelated glyphs. The measured width then disagrees with what `DrawText` renders,
> which makes right-aligned text ragged. `HeaderAlignmentTests` guards against this regressing.

## Package management

Uses Central Package Management (`Directory.Packages.props`). Add new packages there with `<PackageVersion>` before referencing them in individual projects.

Key library dependencies: `FFMpegCore 5.4.0`, `SkiaSharp 2.88.8`.

## CLI

The CLI uses **System.CommandLine 2.0.8**. Run `vcs --help` for the full option list.

Key patterns used in `Program.cs`:
- `new Option<T>(name, alias)` — primary name + alias in one constructor call
- `option.AcceptOnlyFromAmong(...)` — built-in value validation (used for `--format`)
- `option.CustomParser = result => { ... }` — for `TimeIndex?` parsing
- `rootCommand.SetAction(async (parseResult, ct) => { return exitCode; })` — typed async handler
- `parseResult.GetValue(option)` — uniform retrieval for both Options and Arguments
- `rootCommand.Parse(args).InvokeAsync()` — entry point; Ctrl+C is wired automatically

`RootCommand` automatically adds `--version` (reads from the assembly) and `--help`; do not add a `VersionOption` manually.
