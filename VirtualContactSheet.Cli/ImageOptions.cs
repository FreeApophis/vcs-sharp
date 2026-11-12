using VirtualContactSheet.ImageProcessing;

namespace VirtualContactSheet.Cli;

/// <summary>
/// The image-only knobs, which live on <see cref="ImageCollection"/> and its loader rather than
/// on <see cref="ContactSheetOptions"/>. Resolved the same way as the sheet options: library
/// defaults, then config, then CLI args.
/// </summary>
internal sealed record ImageOptions
{
    public ImageFit Fit { get; init; } = ImageFit.Contain;

    public ImageLayout Layout { get; init; } = new GridLayout();

    public bool Recursive { get; init; }

    public ImageSelection Selection { get; init; } = ImageSelection.Sample;
}
