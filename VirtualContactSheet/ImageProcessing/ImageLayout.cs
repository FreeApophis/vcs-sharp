namespace VirtualContactSheet.ImageProcessing;

/// <summary>How the images of a collection are arranged on the sheet.</summary>
public closed record ImageLayout;

/// <summary>The regular grid; cell shape from <see cref="ContactSheetOptions"/> or the first image.</summary>
public sealed record GridLayout : ImageLayout;

/// <summary>A grid of square cells.</summary>
public sealed record SquareLayout : ImageLayout;

/// <summary>
/// Justified rows that keep every image's proportions. The sheet is as wide as a grid of
/// <see cref="ContactSheetOptions.Columns"/>; rows aim at <see cref="ContactSheetOptions.ThumbnailHeight"/>,
/// or by default at the height that fits about <c>Columns</c> images of median shape per row.
/// </summary>
public sealed record MasonryLayout : ImageLayout;
