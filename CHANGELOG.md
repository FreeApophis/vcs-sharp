# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Image contact sheets.** New `ImageCollection` type in `VirtualContactSheet.ImageProcessing`,
  the counterpart of `Video`: build it from a folder (`ImageCollection.FromFolder`) or an explicit
  list of files (`ImageCollection.FromFiles`), then call the same
  `SaveContactSheetAsync` / `BuildContactSheetAsync` with the same `ContactSheetOptions`.
  - `IImageLoader` / `SkiaImageLoader` — decode and scale to the cell size, with
    `ImageFit.Contain` (default, letterboxed), `Cover` (cropped) or `Stretch`.
  - `IImageInfoProvider` / `SkiaImageInfoProvider` — dimensions, format and size read from the
    file header without decoding pixels.
  - `ImageSelection` — `Sample` (default) spreads `Columns × Rows` picks evenly over a large
    collection, mirroring how video frames are distributed; `All` renders every image and lets
    the grid grow past `Rows`.
  - `ImageCollection.Caption` — per-thumbnail caption selector, the file name by default.
  - `ImageHeaderBuilder` — two-column metadata header (folder, image count, total size /
    dimensions, formats).
  - EXIF orientation is applied, so portrait photos are no longer rendered on their side.
- Thumbnail captions that are wider than their cell are now ellipsized instead of overflowing.

### Changed

- **Namespaces split by medium** (breaking, source-level only):
  - `VirtualContactSheet` keeps everything shared: `ContactSheet`, `ContactSheetOptions`,
    `TextStyle`, `SheetFormat`, `HeaderColumns`, `TimeIndex`, `CaptureException`, `FormatNumbers`.
  - `VirtualContactSheet.VideoProcessing` now holds `Video`, `VideoInfo`, `VideoStream`,
    `AudioStream`, `IFrameCapturer`, `FfmpegCapturer`, `IVideoInfoProvider`,
    `FfprobeVideoInfoProvider` and `FrameAnalysis`.
  - `VirtualContactSheet.ImageProcessing` holds the new image types.
  - Existing code keeps working by adding `using VirtualContactSheet.VideoProcessing;`.
- `HeaderBuilder` is renamed `VideoHeaderBuilder` (breaking), to sit beside `ImageHeaderBuilder`.
- `ContactSheet.Thumbnail` now carries a `string? Caption` instead of a `TimeIndex Time`
  (breaking), so the renderer is medium-agnostic. The `Thumbnail(SKBitmap, TimeIndex, bool)`
  constructor still exists and formats the time index as the caption.
- `ContactSheetOptions.Timestamp` documents its broader meaning: it toggles the caption overlay
  (time index for video, file name for images).

## [1.0.1] - 2026-06-27

### Added

- CLI is published as a .NET global tool: `dotnet tool install -g VirtualContactSheet.Cli` (`vcs`).

### Changed

- Bumped SkiaSharp and Tomlyn.

## [1.0.0] - 2026-06-27

### Added

- Initial release: a C# port of [vcs.rb](https://github.com/FreeApophis/vcs.rb).
- `Video` orchestrator — probe → compute times → capture → compose.
- `ContactSheet` / `ContactSheetOptions` — SkiaSharp grid composition with metadata header,
  title, timestamps, drop shadows, polaroid frames and a signature footer.
- `FfmpegCapturer` / `FfprobeVideoInfoProvider` via FFMpegCore, behind `IFrameCapturer` and
  `IVideoInfoProvider`.
- `TimeIndex` — flexible time parsing (`"3m30"`, `"1:22"`, `"90"`, `"1h2m3s"`).
- Blank-frame evasion, highlight band, png/jpg/webp output.
- `vcs` CLI with TOML configuration.
- DejaVu Sans embedded in the library for identical text rendering on every platform.

[Unreleased]: https://github.com/FreeApophis/vcs-sharp/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/FreeApophis/vcs-sharp/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/FreeApophis/vcs-sharp/releases/tag/v1.0.0
