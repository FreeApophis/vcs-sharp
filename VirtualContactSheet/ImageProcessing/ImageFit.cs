namespace VirtualContactSheet.ImageProcessing;

/// <summary>How an image is fitted into a thumbnail cell when their aspect ratios differ.</summary>
public enum ImageFit
{
    /// <summary>Scale to fit entirely inside the cell, keeping the aspect ratio (letterboxed).</summary>
    Contain,

    /// <summary>Scale to fill the cell, keeping the aspect ratio and cropping the overflow.</summary>
    Cover,

    /// <summary>Scale to the cell exactly, distorting the aspect ratio.</summary>
    Stretch,
}
