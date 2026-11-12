# VirtualContactSheet

A C# port of [vcs.rb](https://github.com/FreeApophis/vcs.rb) — generates contact sheets (grids of
thumbnails) from **video files** (ffmpeg/ffprobe for capture) or from **image folders/lists**
(SkiaSharp decoding), composed with SkiaSharp in both cases.

## Build

```
dotnet build
dotnet test
# ffmpeg binaries are git-ignored; without them add -p:BundleFfmpeg=false
dotnet run --project VirtualContactSheet.Cli -- <video> [options]
```

The CLI binary is named `vcs`.

## Development workflow: TDD (Chicago style)

All changes are test-driven, in a strict **red → green → refactor** cycle:

1. **Red** — write one small failing test for the next bit of behaviour. Run it and see it fail
   for the expected reason (a compile error counts as red, but not as the reason).
2. **Green** — write the simplest production code that makes it pass. No speculative extras.
3. **Refactor** — clean up production and test code while all tests stay green. Run the full
   suite (`dotnet test`) before moving on.

Chicago (classicist) style means:

- Test **observable behaviour and resulting state** through the public API, not interactions
  or the internal call sequence.
- Use **real collaborators**. Use test doubles only at true process/IO boundaries (ffmpeg/ffprobe,
  file system, clock), via existing seams such as `IFrameCapturer`, `IImageLoader` and the
  `*InfoProvider` interfaces. Prefer hand-written fakes and stubs over mocking frameworks.
- Let design emerge from the tests; don't add interfaces just to mock something.
- Bug fixes start with a test that reproduces the bug.

### Test stack

- **xUnit v3** (`xunit.v3`) on **Microsoft.Testing.Platform**: `global.json` opts `dotnet test`
  into MTP, and the test project is an `Exe`. There is no `Microsoft.NET.Test.Sdk`,
  `xunit.runner.visualstudio` or `coverlet.collector`, since those are VSTest-only.
- Pass `TestContext.Current.CancellationToken` to anything that takes a `CancellationToken`
  (analyzer `xUnit1051`, an error under CI's `-warnaserror`).
- **Funcky.XUnit.v3** for monad assertions: `Assert.Some(expected, option)`, `Assert.None(option)`,
  `Assert.Ok(...)`, `Assert.Left(...)` / `Assert.Right(...)` (extension members in
  `Funcky.Extensions`, already imported by the Funcky implicit usings).
- **Verify.XunitV3** for snapshot tests (`*.verified.*` files next to the tests). Verify 33+
  requires a SponsorCheck license property: `Directory.Build.props` claims the `OpenSource`
  exemption, which is time-bounded. When `Verify_SponsorshipExemptionUntil` has passed, the
  build fails with `SC040`; move the date forward (at most 12 months).

## Coding style: functional, with Funcky

We write C# in a functional style built on [Funcky](https://github.com/polyadic/funcky).
`FunckyImplicitUsings` (in `Directory.Build.props`) imports `Funcky`, `Funcky.Extensions`,
`Funcky.Monads` and `static Funcky.Functional` globally, so no `using` is needed.

- **`Option<T>` instead of `null`** for values that may be absent. Prefer the Funcky
  `…OrNone` extensions (`FirstOrNone`, `SingleOrNone`, `GetValueOrNone`, `ParseInt32OrNone`,
  …) over `TryXxx` out-parameters, and compose with `Select` / `SelectMany` / `Match` /
  `GetOrElse` instead of `HasValue` checks.
- **`Either<TLeft, TRight>` / `Result<T>`** for expected failures (invalid input, unsupported
  files). Keep exceptions for the truly exceptional, and at existing boundaries (e.g.
  `CaptureException`).
- Prefer **immutability** (records, `init`, `with`), **pure functions** and **expressions**
  (LINQ, switch expressions, pattern matching) over mutable state, loops and statements.
- Use the Funcky `IEnumerable` extensions (`WhereSelect`, `Chunk`, `Interleave`, `Inspect`, …)
  before writing a hand-rolled loop.
- Push side effects (I/O, ffmpeg, rendering) to the edges and keep the core logic pure and
  easy to test.

`Option<T>` is the Funcky one everywhere. System.CommandLine's `Option<T>` clashes with it, so
`CliOptions.cs` aliases the namespace (`using CommandLine = System.CommandLine;`) and writes
`CommandLine.Option<T>`. Do the same in any other file that needs it.

## Project structure

| Project | Purpose |
|---|---|
| `VirtualContactSheet/` | Core library (NuGet-packageable) |
| `VirtualContactSheet.Cli/` | CLI front-end (`vcs` binary) |
| `VirtualContactSheet.Test/` | xUnit v3 test project |

## Namespaces

The library is split by medium; folders match namespaces.

| Namespace | Folder | Contents |
|---|---|---|
| `VirtualContactSheet` | `VirtualContactSheet/` | Everything shared by both media |
| `VirtualContactSheet.VideoProcessing` | `VirtualContactSheet/VideoProcessing/` | Anything ffmpeg/time-index related |
| `VirtualContactSheet.ImageProcessing` | `VirtualContactSheet/ImageProcessing/` | Anything image-file related |

Nested namespaces see the parent implicitly, so library code needs no extra `using`; consumers
(CLI, tests) add `using VirtualContactSheet.VideoProcessing;` / `.ImageProcessing;`. The test
project has both as global usings in its `.csproj`.

Key source files in the library:

Shared:
- `ContactSheet.cs` — SkiaSharp grid layout and rendering. `ContactSheet.Thumbnail` is
  medium-agnostic: an `SKBitmap` plus an optional `string? Caption` (long captions are ellipsized)
- `ContactSheetOptions.cs` — all grid/style settings (`Columns`, `Rows`, `Interval`, `Polaroid`, `SoftShadow`,
  `CornerRadius`, `BorderWidth`, …)
- `BodyLayout.cs`, `LayoutSpacing.cs`, `GridPlanner.cs` — pure placement: a planner turns sizes
  or aspect ratios plus spacing into one rectangle per picture; `ContactSheet.Render(thumbs, body)`
  only draws. Test new layouts as numbers in, rectangles out
- `TimeIndex.cs` — flexible time parser ("3m30", "1:22", "90", "1h2m3s")
- `CaptureException.cs` — thrown when a frame capture or an image load fails
- `MissingFrame.cs` — gray placeholder with a circled warning icon, drawn in the cell of a video
  frame that could not be captured (so a partly corrupt video still yields a sheet)

`VideoProcessing/`:
- `Video.cs` — top-level orchestrator: probe → compute times → capture → compose
- `FfmpegCapturer.cs` — `IFrameCapturer` implementation (FFMpegCore pipe → PNG → SKBitmap)
- `FfprobeVideoInfoProvider.cs` — `FFProbe.AnalyseAsync` → `VideoInfo`
- `FrameAnalysis.cs` — average brightness, for blank-frame evasion
- `VideoHeaderBuilder.cs` — two-column metadata header for a video

`ImageProcessing/`:
- `ImageCollection.cs` — top-level orchestrator: probe → select → load/scale → compose.
  Built via `FromFolder` / `FromFiles`; same `SaveContactSheetAsync(path, options)` shape as `Video`
- `SkiaImageLoader.cs` — `IImageLoader` implementation: decode, straighten EXIF orientation,
  fit into the cell (`ImageFit.Contain` / `Cover` / `Stretch`). Every thumbnail comes back at the
  same size, which is what the grid layout assumes
- `SkiaImageInfoProvider.cs` — `SKCodec` header read → `ImageInfo` (no pixel decoding)
- `ImageOrientation.cs` — EXIF origin → upright dimensions + canvas matrix
- `ImageHeaderBuilder.cs` — two-column metadata header for a collection
- `ImageSelection.cs` — `Sample` (evenly spread over the collection) vs `All`
- `ImageLayout.cs` — closed hierarchy for how images are arranged: `GridLayout` (default),
  `SquareLayout` (square cells; the default loader then crops with `ImageFit.Cover`)

`ContactSheetOptions` is shared; its time-based members (`Interval`, `From`, `To`, `Highlights`,
blank-frame evasion) apply to video only, and `Timestamp` toggles the caption overlay for both.

## Target framework

`Directory.Build.props` sets `TargetFramework` to `net10.0` for all projects.

## Runtime dependencies / bundled ffmpeg

ffmpeg binaries (Windows x64) live in `tools/ffmpeg/win-x64/` and are copied to the CLI build
output by `VirtualContactSheet.Cli.csproj` via a Windows-conditional `<Content>` rule. The CLI
auto-detects them on startup, so no global PATH install is needed.

The binaries are git-ignored (~100 MB). Reproduce them with:
```
pwsh tools/download-ffmpeg.ps1          # or: powershell -File tools\download-ffmpeg.ps1
```

Rules that keep this manageable — don't regress them:

- **Never commit the binaries, never ship them in a package.** The library invokes ffmpeg as a
  separate process rather than linking it, which is what keeps ffmpeg's GPL off this codebase;
  the gyan.dev "essentials" builds are GPL, so redistributing one would pull in source-offer
  obligations. Both `dotnet pack` steps in `publish.yml` pass `-p:BundleFfmpeg=false`.
- **Bundling is optional, never required.** Each `<Content>` item is guarded by `Exists()` and a
  `ReportMissingBundledFfmpeg` target prints a hint, so a clean checkout builds without the
  download (the CLI then falls back to `PATH`). Use `Message`, not `Warning`, in that target: CI
  builds with a bare `-warnaserror`, which promotes MSBuild warnings to errors.
- **The version is pinned in `tools/ffmpeg.json`** (version + URL + SHA-256), and the script
  refuses a hash mismatch. Bump all three together; an unpinned download would silently change
  the binaries that capture frames. gyan.dev prunes old packages, so a stale pin fails loudly with
  a 404 asking for a bump.
- CI caches `tools/ffmpeg` keyed on `hashFiles('tools/ffmpeg.json')`; the script no-ops on a cache
  hit by comparing `tools/ffmpeg/<rid>/.version`.

To use bundled binaries from the library:
```csharp
var video = new Video("movie.mkv", ffBinaryFolder: AppContext.BaseDirectory);
```

(Image contact sheets need no ffmpeg at all.)

To add Linux/macOS support: add the platform to `tools/ffmpeg.json` (url + SHA-256 + binary
names), run `pwsh tools/download-ffmpeg.ps1 -Platform linux-x64` to populate
`tools/ffmpeg/linux-x64/`, and add matching `<Content>` blocks with an `IsOSPlatform('Linux')`
condition in the CLI `.csproj`. Prefer an LGPL build there (e.g. BtbN's `*-lgpl-shared`) if
redistribution ever becomes interesting.

## Bundled font

DejaVu Sans (regular + bold) is embedded in the core library as assembly resources
(`VirtualContactSheet/Fonts/*.ttf`, `<EmbeddedResource>` in the `.csproj`; license in
`Fonts/LICENSE`). `BundledFonts` loads them once (cached `SKTypeface`), and `ContactSheet.CreateFont`
uses them by default — matching the original vcs.rb and making text render identically on every
platform. Per-`TextStyle` `FontFile` (path) or `FontFamily` (system font name) still override it.

## Known SkiaSharp API quirk

In the SkiaSharp version used here on .NET 10, the string/char-span/encoding overloads of `SKFont.MeasureText`
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

Key library dependencies: `FFMpegCore 6.0.0`, `SkiaSharp 4.156.0`, `Funcky 3.8.0`.

## CLI

The CLI uses **System.CommandLine 3.0.0**. Run `vcs --help` for the full option list.

Key patterns used in `Program.cs` / `CliOptions.cs`:
- `new CommandLine.Option<T>(name, alias)` — primary name + alias in one constructor call
  (aliased because of the clash with Funcky's `Option<T>`, see *Coding style*)
- `option.AcceptOnlyFromAmong(...)` — built-in value validation (used for `--format`)
- `option.CustomParser = result => { ... }` — for `TimeIndex?` parsing
- `rootCommand.SetAction(async (parseResult, ct) => { return exitCode; })` — typed async handler
- `parseResult.GetValue(option)` — uniform retrieval for both Options and Arguments
- `rootCommand.Parse(args).InvokeAsync()` — entry point; Ctrl+C is wired automatically

`RootCommand` automatically adds `--version` (reads from the assembly) and `--help`; do not add a `VersionOption` manually.

The single root command serves both media — there are no subcommands. `InputResolver` turns the
positional arguments into a list of `SheetInput`s (one per output sheet), which is where the
asymmetry lives: a video and a directory each make their own sheet, but N loose image files make
one, so they are coalesced into a group at the position of the first. Detection order is
`Directory.Exists`, then (for an existing file) `ImageCollection.IsSupported`, then video — so an
unrecognised extension still resolves to video, as it did before image support. A path that
exists as neither file nor folder is an error up front (`Folder not found` / `File or folder not
found`), so resolver tests must create the files they resolve. `OptionScopeValidator` then rejects
options that cannot apply to any resolved input, and `SheetRunner` switches on the input type.

`SheetInput` is a C# 15 **closed hierarchy** (`SheetInput.cs`): `VideoInput`, and the closed
`ImageInput` with `ImageFolderInput` / `LooseImagesInput`. Each case carries only its own data, so
image-only code takes an `ImageInput` and video code a `VideoInput`. Switch over these without a
`_` discard arm: the compiler then checks exhaustiveness, and adding a new input kind fails the
build (CS8509 under `-warnaserror`) at every switch that does not handle it yet. A `closed` type
is implicitly abstract; don't mark it `abstract` too (CS9384).

## Changelog

`CHANGELOG.md` follows Keep a Changelog; user-visible changes go under `## [Unreleased]`.
