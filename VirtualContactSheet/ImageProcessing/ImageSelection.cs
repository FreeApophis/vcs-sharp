namespace VirtualContactSheet.ImageProcessing;

/// <summary>How a collection with more images than grid cells is reduced to fit the sheet.</summary>
public enum ImageSelection
{
    /// <summary>Spread <c>Columns × Rows</c> picks evenly over the collection (mirrors how video frames are sampled).</summary>
    Sample,

    /// <summary>Use every image, letting the grid grow past <c>Rows</c>.</summary>
    All,
}
