using SkiaSharp;

namespace VideoContactSheet;

/// <summary>Styling for a single text region (header, title, timestamp, signature).</summary>
public sealed class TextStyle
{
    public string? FontFamily { get; set; }

    public string? FontFile { get; set; }

    public float Size { get; set; } = 14;

    public SKColor Color { get; set; } = SKColors.Black;

    public SKColor Background { get; set; } = SKColors.Transparent;

    public bool Bold { get; set; }

    public TextStyle Clone() => (TextStyle)MemberwiseClone();
}
