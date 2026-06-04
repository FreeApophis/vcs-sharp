using SkiaSharp;

namespace VideoContactSheet;

/// <summary>Builds the composed contact-sheet image from captured frames using SkiaSharp.</summary>
public sealed class ContactSheet
{
    private readonly ContactSheetOptions _options;

    public ContactSheet(ContactSheetOptions options) => _options = options;

    public sealed record Thumbnail(SKBitmap Image, TimeIndex Time, bool IsHighlight = false);

    /// <summary>Optional two-column metadata header injected by the orchestrator.</summary>
    public HeaderColumns? HeaderOverride { get; set; }

    private abstract record Element;

    private sealed record ColorRect(SKRect Bounds, SKColor Color) : Element;

    private sealed record BlurShadow(SKRect Bounds, float Blur) : Element;

    private sealed record BitmapEl(SKBitmap Image, SKRect Dest) : Element;

    private sealed record TextEl(string Content, SKFont Font, SKColor Color, float X, float Y) : Element;

    private sealed record Sheet(int Width, int Height, IReadOnlyList<Element> Elements);

    private sealed record GridSpec(int ThumbW, int ThumbH, int CellW, int CellH, int Margin, int Columns);

    public byte[] Render(IReadOnlyList<Thumbnail> thumbnails, string? title = null)
    {
        title ??= _options.Title;

        using var titleFont = CreateFont(_options.TitleStyle);
        using var headerFont = CreateFont(_options.HeaderStyle);
        using var sigFont = CreateFont(_options.SignatureStyle);
        using var tsFont = CreateFont(_options.TimestampStyle);

        var sheet = Compose(thumbnails, title, titleFont, headerFont, sigFont, tsFont);

        using var surface = SKSurface.Create(new SKImageInfo(sheet.Width, sheet.Height));
        var canvas = surface.Canvas;
        canvas.Clear(_options.SheetBackground);

        foreach (var element in sheet.Elements)
        {
            Draw(canvas, element);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(EncodeFormat(_options.Format), _options.JpegQuality);
        return data.ToArray();
    }

    private static void Draw(SKCanvas canvas, Element element)
    {
        switch (element)
        {
            case ColorRect(var b, var c) when c.Alpha > 0:
            {
                using var p = new SKPaint { Color = c };
                canvas.DrawRect(b, p);
                break;
            }

            case BlurShadow(var b, var blur):
            {
                using var p = new SKPaint
                {
                    Color = new SKColor(0, 0, 0, 140),
                    ImageFilter = SKImageFilter.CreateBlur(blur, blur),
                    IsAntialias = true,
                };
                canvas.DrawRect(b, p);
                break;
            }

            case BitmapEl(var bmp, var dest):
                canvas.DrawBitmap(bmp, dest);
                break;

            case TextEl(var text, var font, var color, var x, var y):
            {
                using var p = new SKPaint { Color = color, IsAntialias = true };
                canvas.DrawText(text, x, y, font, p);
                break;
            }
        }
    }

    private Sheet Compose(
        IReadOnlyList<Thumbnail> thumbnails,
        string? title,
        SKFont titleFont,
        SKFont headerFont,
        SKFont sigFont,
        SKFont tsFont)
    {
        var o = _options;
        var highlights = thumbnails.Where(t => t.IsHighlight).ToList();
        var regular = thumbnails.Where(t => !t.IsHighlight).ToList();

        int columns = Math.Max(1, o.Columns);
        int padding = Math.Max(0, o.Padding);
        int shadow = o.SoftShadow ? Math.Max(0, o.ShadowSize) : 0;
        int polaroidPad = o.Polaroid ? 12 : 0;
        int polaroidBot = o.Polaroid ? 36 : 0;

        var sample = (regular.FirstOrDefault() ?? highlights.FirstOrDefault())?.Image
                     ?? throw new InvalidOperationException("No thumbnails to render.");

        var grid = new GridSpec(
            ThumbW: sample.Width,
            ThumbH: sample.Height,
            CellW: sample.Width + (2 * polaroidPad),
            CellH: sample.Height + polaroidPad + polaroidBot,
            Margin: padding + shadow,
            Columns: columns);

        var header = o.ShowHeader ? (HeaderOverride ?? HeaderColumns.Empty) : HeaderColumns.Empty;
        int hdrRows = Math.Max(header.Left.Count, header.Right.Count);

        float titleH = string.IsNullOrEmpty(title) ? 0f : LineHeight(titleFont) + (2 * padding) + 8;
        float headerH = hdrRows > 0 ? (hdrRows * LineHeight(headerFont)) + (2 * padding) + 8 : 0f;
        float sigH = o.ShowSignature && !string.IsNullOrEmpty(o.Signature)
            ? LineHeight(sigFont) + (2 * padding) + 6 : 0f;

        int hlRows = RowCount(highlights.Count, columns);
        int regRows = RowCount(regular.Count, columns);
        float hlBandH = hlRows > 0
            ? (2 * grid.Margin) + (hlRows * grid.CellH) + ((hlRows - 1) * padding)
            : 0f;
        float gridH = (2 * grid.Margin) + (regRows * grid.CellH) + ((regRows - 1) * padding);

        int width = (2 * grid.Margin) + (columns * grid.CellW) + ((columns - 1) * padding);
        int height = (int)Math.Ceiling(titleH + headerH + hlBandH + gridH + sigH);

        float titleY = 0f;
        float headerY = titleY + titleH;
        float hlY = headerY + headerH;
        float gridY = hlY + hlBandH;
        float sigY = height - sigH;

        var elements = Enumerable.Empty<Element>()
            .Concat(TitleBand(title, titleFont, o, width, titleY, titleH))
            .Concat(HeaderBand(header, headerFont, o, width, padding, headerY, headerH))
            .Concat(HighlightBand(highlights, grid, o, tsFont, padding, shadow, polaroidPad, polaroidBot, width, hlY, hlBandH))
            .Concat(GridBand(regular, grid, o, tsFont, padding, shadow, polaroidPad, polaroidBot, gridY))
            .Concat(SignatureBand(o, sigFont, width, height, sigY, sigH));

        return new Sheet(width, height, [.. elements]);
    }

    private static IEnumerable<Element> TitleBand(
        string? title,
        SKFont font,
        ContactSheetOptions o,
        int width,
        float y,
        float h)
    {
        if (h == 0 || string.IsNullOrEmpty(title))
        {
            yield break;
        }

        yield return new ColorRect(new SKRect(0, y, width, y + h), o.TitleStyle.Background);
        yield return CenteredTextEl(title, font, o.TitleStyle.Color, width / 2f, y + (h / 2f));
    }

    private static IEnumerable<Element> HeaderBand(
        HeaderColumns header,
        SKFont font,
        ContactSheetOptions o,
        int width,
        int padding,
        float y,
        float h)
    {
        if (h == 0)
        {
            yield break;
        }

        yield return new ColorRect(new SKRect(0, y, width, y + h), o.HeaderStyle.Background);

        float lh = LineHeight(font);
        float ly = y + padding + (lh * 0.8f);
        int rows = Math.Max(header.Left.Count, header.Right.Count);

        for (int row = 0; row < rows; row++, ly += lh)
        {
            if (row < header.Left.Count)
            {
                yield return new TextEl(header.Left[row], font, o.HeaderStyle.Color, padding + 4, ly);
            }

            if (row < header.Right.Count)
            {
                float tw = MeasureTextWidth(font, header.Right[row]);
                yield return new TextEl(header.Right[row], font, o.HeaderStyle.Color, width - padding - 4 - tw, ly);
            }
        }
    }

    private static IEnumerable<Element> HighlightBand(
        IReadOnlyList<Thumbnail> items,
        GridSpec grid,
        ContactSheetOptions o,
        SKFont tsFont,
        int padding,
        int shadow,
        int polaroidPad,
        int polaroidBot,
        int width,
        float y,
        float h)
    {
        if (h == 0)
        {
            yield break;
        }

        yield return new ColorRect(new SKRect(0, y, width, y + h), o.HighlightBackground);

        foreach (var e in GridBand(items, grid, o, tsFont, padding, shadow, polaroidPad, polaroidBot, y))
        {
            yield return e;
        }
    }

    private static IEnumerable<Element> GridBand(
        IReadOnlyList<Thumbnail> items,
        GridSpec grid,
        ContactSheetOptions o,
        SKFont tsFont,
        int padding,
        int shadow,
        int polaroidPad,
        int polaroidBot,
        float startY)
        => items.SelectMany((thumb, i) =>
        {
            float cellX = grid.Margin + ((i % grid.Columns) * (grid.CellW + padding));
            float cellY = startY + grid.Margin + ((i / grid.Columns) * (grid.CellH + padding));
            float imgX = cellX + polaroidPad;
            float imgY = cellY + polaroidPad;
            return ThumbnailElements(thumb, grid, o, tsFont, shadow, polaroidPad, cellX, cellY, imgX, imgY);
        });

    private static IEnumerable<Element> ThumbnailElements(
        Thumbnail thumb,
        GridSpec grid,
        ContactSheetOptions o,
        SKFont tsFont,
        int shadow,
        int polaroidPad,
        float cellX,
        float cellY,
        float imgX,
        float imgY)
    {
        if (shadow > 0)
        {
            float sx = o.Polaroid ? cellX : imgX;
            float sy = o.Polaroid ? cellY : imgY;
            float sw = o.Polaroid ? grid.CellW : grid.ThumbW;
            float sh = o.Polaroid ? grid.CellH : grid.ThumbH;
            yield return new BlurShadow(
                SKRect.Create(sx + (shadow / 2f), sy + (shadow / 2f), sw, sh),
                shadow / 2f);
        }

        if (o.Polaroid)
        {
            yield return new ColorRect(SKRect.Create(cellX, cellY, grid.CellW, grid.CellH), SKColors.White);
        }

        yield return new BitmapEl(thumb.Image, SKRect.Create(imgX, imgY, grid.ThumbW, grid.ThumbH));

        if (o.Timestamp)
        {
            foreach (var e in TimestampElements(thumb.Time.ToTimestamp(), tsFont, o, imgX, imgY, grid.ThumbW, grid.ThumbH))
            {
                yield return e;
            }
        }
    }

    private static IEnumerable<Element> TimestampElements(
        string text, SKFont font, ContactSheetOptions o, float imgX, float imgY, float w, float h)
    {
        float textW = MeasureTextWidth(font, text);
        float lh = LineHeight(font);
        float pad = 4f;
        float boxW = textW + (2 * pad);
        float boxH = lh + pad;
        float bx = imgX + w - boxW - 4;
        float by = imgY + h - boxH - 4;
        var m = font.Metrics;
        float bl = by + ((boxH - (m.Descent - m.Ascent)) / 2) - m.Ascent;

        yield return new ColorRect(SKRect.Create(bx, by, boxW, boxH), o.TimestampStyle.Background);
        yield return new TextEl(text, font, o.TimestampStyle.Color, bx + pad, bl);
    }

    private static IEnumerable<Element> SignatureBand(
        ContactSheetOptions o, SKFont font, int width, int height, float y, float h)
    {
        if (h == 0)
        {
            yield break;
        }

        yield return new ColorRect(new SKRect(0, y, width, height), o.SignatureStyle.Background);
        yield return CenteredTextEl(o.Signature!, font, o.SignatureStyle.Color, width / 2f, y + (h / 2f));
    }

    private static TextEl CenteredTextEl(string text, SKFont font, SKColor color, float cx, float cy)
    {
        var m = font.Metrics;
        return new TextEl(
            text,
            font,
            color,
            cx - (MeasureTextWidth(font, text) / 2f),
            cy - ((m.Ascent + m.Descent) / 2));
    }

    private static SKFont CreateFont(TextStyle style)
    {
        SKTypeface typeface;
        if (!string.IsNullOrEmpty(style.FontFile) && File.Exists(style.FontFile))
        {
            typeface = SKTypeface.FromFile(style.FontFile);
        }
        else if (!string.IsNullOrEmpty(style.FontFamily))
        {
            typeface = SKTypeface.FromFamilyName(
                style.FontFamily,
                style.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright);
        }
        else
        {
            typeface = BundledFonts.ForWeight(style.Bold);
        }

        return new SKFont(typeface ?? SKTypeface.Default, style.Size);
    }

    private static float LineHeight(SKFont font)
    {
        var m = font.Metrics;
        return m.Descent - m.Ascent + m.Leading;
    }

    private static int RowCount(int count, int columns) =>
        (int)Math.Ceiling(count / (double)columns);

    // SkiaSharp 2.88.x on .NET 10 only exposes glyph-ID overloads of MeasureText/GetGlyphs.
    // Map text to real glyph IDs via Unicode codepoints so measured width matches DrawText.
    private static float MeasureTextWidth(SKFont font, string text)
    {
        if (text.Length == 0)
        {
            return 0;
        }

        var codepoints = new List<int>(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                codepoints.Add(char.ConvertToUtf32(text[i], text[i + 1]));
                i++;
            }
            else
            {
                codepoints.Add(text[i]);
            }
        }

        var glyphs = new ushort[codepoints.Count];
        font.GetGlyphs(codepoints.ToArray(), glyphs);
        return font.MeasureText(glyphs);
    }

    private static SKEncodedImageFormat EncodeFormat(SheetFormat format) => format switch
    {
        SheetFormat.Jpg => SKEncodedImageFormat.Jpeg,
        SheetFormat.Webp => SKEncodedImageFormat.Webp,
        _ => SKEncodedImageFormat.Png,
    };
}
