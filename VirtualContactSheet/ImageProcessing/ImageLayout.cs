namespace VirtualContactSheet.ImageProcessing;

/// <summary>How the images of a collection are arranged on the sheet.</summary>
public closed record ImageLayout;

/// <summary>The regular grid; cell shape from <see cref="ContactSheetOptions"/> or the first image.</summary>
public sealed record GridLayout : ImageLayout;

/// <summary>A grid of square cells.</summary>
public sealed record SquareLayout : ImageLayout;
