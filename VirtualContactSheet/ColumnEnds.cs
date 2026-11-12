namespace VirtualContactSheet;

/// <summary>How the bottoms of masonry columns, which come out at different heights, are finished.</summary>
public enum ColumnEnds
{
    /// <summary>Every column ends at the same height; each image gives or takes a little, cropped to fit.</summary>
    Cropped,

    /// <summary>Every column keeps its natural height, so the bottom edge is uneven.</summary>
    Ragged,
}
