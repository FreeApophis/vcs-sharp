using SkiaSharp;

namespace VirtualContactSheet;

/// <summary>
/// The placeholder drawn in place of a frame that could not be captured: a gray cell with a
/// circled exclamation mark, so the gap is visible on the sheet and the grid keeps its order.
/// </summary>
public static class MissingFrame
{
    public static readonly SKColor Background = new(0x80, 0x80, 0x80);

    public static readonly SKColor IconColor = new(0xE8, 0xE8, 0xE8);

    public static SKBitmap Render(int width, int height)
    {
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(Background);

        float cx = width / 2f;
        float cy = height / 2f;
        float radius = Math.Min(width, height) * 0.1f;
        float stroke = radius * 0.16f;

        using var ring = new SKPaint { Color = IconColor, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = stroke };
        using var fill = new SKPaint { Color = IconColor, IsAntialias = true, Style = SKPaintStyle.Fill };

        canvas.DrawCircle(cx, cy, radius, ring);

        // Exclamation mark: a bar through the centre and a dot below it.
        canvas.DrawRoundRect(
            SKRect.Create(cx - (stroke / 2), cy - (radius * 0.55f), stroke, radius * 0.7f),
            stroke / 2,
            stroke / 2,
            fill);
        canvas.DrawCircle(cx, cy + (radius * 0.45f), stroke * 0.6f, fill);

        return bitmap;
    }
}
