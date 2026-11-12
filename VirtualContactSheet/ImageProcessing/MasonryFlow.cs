namespace VirtualContactSheet.ImageProcessing;

/// <summary>Which way a masonry layout is filled.</summary>
public enum MasonryFlow
{
    /// <summary>Justified rows of equal height, in reading order.</summary>
    Rows,

    /// <summary>Columns of equal width, each image placed in the shortest column.</summary>
    Columns,
}
