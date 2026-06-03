using System.Runtime.InteropServices;
using SkiaSharp;

namespace VideoContactSheet;

/// <summary>Builds the composed contact-sheet image from captured frames using SkiaSharp.</summary>
public sealed class ContactSheet
{
    private readonly ContactSheetOptions _options;

    public ContactSheet(ContactSheetOptions options) => _options = options;

    public sealed record Thumbnail(SKBitmap Image, TimeIndex Time, bool IsHighlight = false);

    /// <summary>Optional metadata lines injected by the orchestrator (filename, size, codecs...).</summary>
    public string[]? HeaderLinesOverride { get; set; }

    /// <summary>
    /// Compose the supplied thumbnails into a single sheet image and return the encoded bytes.
    /// </summary>
    public byte[] Render(IReadOnlyList<Thumbnail> thumbnails, string? title = null)
    {
        var o = _options;
        title ??= o.Title;

        var highlights = thumbnails.Where(t => t.IsHighlight).ToList();
        var regular = thumbnails.Where(t => !t.IsHighlight).ToList();

        int columns = Math.Max(1, o.Columns);
        int padding = Math.Max(0, o.Padding);
        int shadow = o.SoftShadow ? Math.Max(0, o.ShadowSize) : 0;
        int polaroidPad = o.Polaroid ? 12 : 0;
        int polaroidBottom = o.Polaroid ? 36 : 0;

        // Determine cell size from the first thumbnail (all captured at the same width).
        var sample = (regular.FirstOrDefault() ?? highlights.FirstOrDefault())?.Image
                     ?? throw new InvalidOperationException("No thumbnails to render.");
        int thumbW = sample.Width;
        int thumbH = sample.Height;

        int cellW = thumbW + (2 * polaroidPad) + shadow;
        int cellH = thumbH + polaroidPad + polaroidBottom + shadow;

        int regularRows = (int)Math.Ceiling(regular.Count / (double)columns);
        int highlightRows = highlights.Count > 0
            ? (int)Math.Ceiling(highlights.Count / (double)columns)
            : 0;

        // Measure header / title / footer bands.
        using var titleFont = CreateFont(o.TitleStyle);
        using var headerFont = CreateFont(o.HeaderStyle);
        using var sigFont = CreateFont(o.SignatureStyle);

        var headerLines = o.ShowHeader ? BuildHeaderLines() : Array.Empty<string>();
        float titleH = !string.IsNullOrEmpty(title) ? LineHeight(titleFont) + (2 * padding) + 8 : 0;
        float headerH = headerLines.Length > 0
            ? (headerLines.Length * LineHeight(headerFont)) + (2 * padding) + 8
            : 0;
        float sigH = (o.ShowSignature && !string.IsNullOrEmpty(o.Signature))
            ? LineHeight(sigFont) + (2 * padding) + 6
            : 0;
        float highlightBandH = highlightRows > 0 ? (highlightRows * (cellH + padding)) + padding : 0;

        int gridW = (columns * cellW) + ((columns + 1) * padding);
        int width = gridW;
        float gridH = (regularRows * (cellH + padding)) + padding;

        float totalH = titleH + headerH + highlightBandH + gridH + sigH;

        var imageInfo = new SKImageInfo(width, (int)Math.Ceiling(totalH));
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;
        canvas.Clear(o.SheetBackground);

        float y = 0;

        // Title band.
        if (titleH > 0)
        {
            DrawBand(canvas, new SKRect(0, y, width, y + titleH), o.TitleStyle.Background);
            DrawTextCentered(canvas, title!, titleFont, o.TitleStyle.Color, width / 2f, y + (titleH / 2f));
            y += titleH;
        }

        // Header band (metadata).
        if (headerH > 0)
        {
            DrawBand(canvas, new SKRect(0, y, width, y + headerH), o.HeaderStyle.Background);
            float ly = y + padding + (LineHeight(headerFont) * 0.8f);
            foreach (var line in headerLines)
            {
                DrawTextLeft(canvas, line, headerFont, o.HeaderStyle.Color, padding + 4, ly);
                ly += LineHeight(headerFont);
            }

            y += headerH;
        }

        // Highlight band.
        if (highlightBandH > 0)
        {
            DrawBand(canvas, new SKRect(0, y, width, y + highlightBandH), o.HighlightBackground);
            y = DrawGrid(
                canvas,
                highlights,
                columns,
                padding,
                shadow,
                polaroidPad,
                polaroidBottom,
                cellW,
                cellH,
                thumbW,
                thumbH,
                y);
        }

        // Main grid.
        DrawGrid(
            canvas,
            regular,
            columns,
            padding,
            shadow,
            polaroidPad,
            polaroidBottom,
            cellW,
            cellH,
            thumbW,
            thumbH,
            y);
        y += gridH;

        // Signature footer.
        if (sigH > 0)
        {
            float fy = totalH - sigH;
            DrawBand(canvas, new SKRect(0, fy, width, totalH), o.SignatureStyle.Background);
            DrawTextRight(
                canvas,
                o.Signature!,
                sigFont,
                o.SignatureStyle.Color,
                width - padding - 4,
                fy + (sigH / 2f));
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(EncodeFormat(o.Format), o.JpegQuality);
        return data.ToArray();
    }

    private float DrawGrid(
        SKCanvas canvas,
        IReadOnlyList<Thumbnail> items,
        int columns,
        int padding,
        int shadow,
        int polaroidPad,
        int polaroidBottom,
        int cellW,
        int cellH,
        int thumbW,
        int thumbH,
        float startY)
    {
        var o = _options;
        using var tsFont = CreateFont(o.TimestampStyle);

        float y = startY + padding;
        for (int i = 0; i < items.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;
            float cellX = padding + (col * (cellW + padding));
            float cellY = y + (row * (cellH + padding));

            float imgX = cellX + polaroidPad;
            float imgY = cellY + polaroidPad;

            // Drop shadow.
            if (shadow > 0)
            {
                using var shadowPaint = new SKPaint
                {
                    Color = new SKColor(0, 0, 0, 140),
                    ImageFilter = SKImageFilter.CreateBlur(shadow / 2f, shadow / 2f),
                    IsAntialias = true,
                };
                var shadowRect = SKRect.Create(imgX + (shadow / 2f), imgY + (shadow / 2f), thumbW, thumbH);
                canvas.DrawRect(shadowRect, shadowPaint);
            }

            // Polaroid white frame.
            if (o.Polaroid)
            {
                using var frame = new SKPaint { Color = SKColors.White, IsAntialias = true };
                var frameRect = SKRect.Create(
                    imgX - polaroidPad,
                    imgY - polaroidPad,
                    thumbW + (2 * polaroidPad),
                    thumbH + polaroidPad + polaroidBottom);
                canvas.DrawRect(frameRect, frame);
            }

            // The frame image.
            var dest = SKRect.Create(imgX, imgY, thumbW, thumbH);
            canvas.DrawBitmap(items[i].Image, dest);

            // Timestamp overlay (bottom-right of the thumbnail).
            if (o.Timestamp)
            {
                DrawTimestamp(canvas, items[i].Time.ToTimestamp(), tsFont, imgX, imgY, thumbW, thumbH);
            }
        }

        int rows = (int)Math.Ceiling(items.Count / (double)columns);
        return startY + (rows * (cellH + padding)) + padding;
    }

    private void DrawTimestamp(
        SKCanvas canvas,
        string text,
        SKFont font,
        float imgX,
        float imgY,
        float w,
        float h)
    {
        var o = _options;
        using var fill = new SKPaint { Color = o.TimestampStyle.Color, IsAntialias = true };
        float textW = MeasureTextWidth(font, text);
        float lh = LineHeight(font);
        float boxPad = 4;
        float boxW = textW + (2 * boxPad);
        float boxH = lh + boxPad;
        float bx = imgX + w - boxW - 4;
        float by = imgY + h - boxH - 4;

        using var bg = new SKPaint { Color = o.TimestampStyle.Background, IsAntialias = true };
        canvas.DrawRect(SKRect.Create(bx, by, boxW, boxH), bg);

        var metrics = font.Metrics;
        float baseline = by + ((boxH - (metrics.Descent - metrics.Ascent)) / 2) - metrics.Ascent;
        canvas.DrawText(text, bx + boxPad, baseline, font, fill);
    }

    private string[] BuildHeaderLines()
    {
        return HeaderLinesOverride ?? Array.Empty<string>();
    }

    // Skia helpers.
    private static SKFont CreateFont(TextStyle style)
    {
        SKTypeface typeface;
        if (!string.IsNullOrEmpty(style.FontFile) && File.Exists(style.FontFile))
        {
            typeface = SKTypeface.FromFile(style.FontFile);
        }
        else
        {
            typeface = SKTypeface.FromFamilyName(
                style.FontFamily ?? "sans-serif",
                style.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright);
        }

        return new SKFont(typeface ?? SKTypeface.Default, style.Size);
    }

    private static float LineHeight(SKFont font)
    {
        var m = font.Metrics;
        return m.Descent - m.Ascent + m.Leading;
    }

    private static void DrawBand(SKCanvas canvas, SKRect rect, SKColor color)
    {
        if (color.Alpha == 0)
        {
            return;
        }

        using var paint = new SKPaint { Color = color, IsAntialias = false };
        canvas.DrawRect(rect, paint);
    }

    private static void DrawTextCentered(
        SKCanvas canvas,
        string text,
        SKFont font,
        SKColor color,
        float cx,
        float cy)
    {
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        float w = MeasureTextWidth(font, text);
        var m = font.Metrics;
        float baseline = cy - ((m.Ascent + m.Descent) / 2);
        canvas.DrawText(text, cx - (w / 2), baseline, font, paint);
    }

    private static void DrawTextLeft(
        SKCanvas canvas,
        string text,
        SKFont font,
        SKColor color,
        float x,
        float baseline)
    {
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        canvas.DrawText(text, x, baseline, font, paint);
    }

    private static void DrawTextRight(
        SKCanvas canvas,
        string text,
        SKFont font,
        SKColor color,
        float right,
        float cy)
    {
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        float w = MeasureTextWidth(font, text);
        var m = font.Metrics;
        float baseline = cy - ((m.Ascent + m.Descent) / 2);
        canvas.DrawText(text, right - w, baseline, font, paint);
    }

    private static float MeasureTextWidth(SKFont font, string text)
        => font.MeasureText(MemoryMarshal.Cast<char, ushort>(text.AsSpan()));

    private static SKEncodedImageFormat EncodeFormat(SheetFormat format) => format switch
    {
        SheetFormat.Jpg => SKEncodedImageFormat.Jpeg,
        SheetFormat.Webp => SKEncodedImageFormat.Webp,
        _ => SKEncodedImageFormat.Png,
    };
}
