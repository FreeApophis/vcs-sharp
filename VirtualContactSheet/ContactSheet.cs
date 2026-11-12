using SkiaSharp;

namespace VirtualContactSheet;

/// <summary>Builds the composed contact-sheet image from captured frames using SkiaSharp.</summary>
public sealed class ContactSheet
{
    private readonly ContactSheetOptions _options;

    public ContactSheet(ContactSheetOptions options) => _options = options;

    /// <summary>
    /// One cell of the sheet: the image plus an optional caption drawn in its lower-right corner
    /// (a timestamp for video frames, the file name for images).
    /// </summary>
    public sealed record Thumbnail(SKBitmap Image, string? Caption = null, bool IsHighlight = false)
    {
        /// <summary>Convenience overload for video frames: the caption is the formatted time index.</summary>
        public Thumbnail(SKBitmap image, TimeIndex time, bool isHighlight = false)
            : this(image, time.ToTimestamp(), isHighlight)
        {
        }
    }

    /// <summary>Optional two-column metadata header injected by the orchestrator.</summary>
    public HeaderColumns? HeaderOverride { get; set; }

    private abstract record Element;

    private sealed record ColorRect(SKRect Bounds, SKColor Color) : Element;

    private sealed record BlurShadow(SKRect Bounds, float Blur, float Radius) : Element;

    /// <summary>Draws <paramref name="Inner"/> clipped to a rounded rectangle.</summary>
    private sealed record RoundedClip(SKRect Bounds, float Radius, IReadOnlyList<Element> Inner) : Element;

    /// <summary>A border stroked just inside <paramref name="Bounds"/>.</summary>
    private sealed record Border(SKRect Bounds, float Radius, float Width, SKColor Color) : Element;

    private sealed record BitmapEl(SKBitmap Image, SKRect Dest) : Element;

    private sealed record TextEl(string Content, SKFont Font, SKColor Color, float X, float Y) : Element;

    private sealed record Sheet(int Width, int Height, IReadOnlyList<Element> Elements);

    private sealed record GridSpec(int ThumbW, int ThumbH, int CellW, int CellH, int Margin, int Columns);

    /// <summary>Renders the thumbnails as a regular grid, every cell the size of the first thumbnail.</summary>
    public byte[] Render(IReadOnlyList<Thumbnail> thumbnails, string? title = null)
    {
        var regular = thumbnails.Where(t => !t.IsHighlight).ToList();
        var sample = SampleImage(thumbnails);
        var body = GridPlanner.Plan(regular.Count, _options.Columns, new SKSizeI(sample.Width, sample.Height), LayoutSpacing.For(_options));

        return Render(thumbnails, body, title);
    }

    /// <summary>
    /// Renders the regular (non-highlight) thumbnails at the places <paramref name="body"/> gives
    /// them, in order; the sheet is as wide as the body. Plan the body with the spacing from
    /// <see cref="LayoutSpacing.For"/>, so there is room for shadows and polaroid frames.
    /// </summary>
    public byte[] Render(IReadOnlyList<Thumbnail> thumbnails, BodyLayout body, string? title = null)
    {
        int regularCount = thumbnails.Count(t => !t.IsHighlight);
        if (body.Pictures.Count != regularCount)
        {
            throw new ArgumentException(
                $"The body layout places {body.Pictures.Count} pictures, but there are {regularCount} thumbnails.",
                nameof(body));
        }

        title ??= _options.Title;

        using var titleFont = CreateFont(_options.TitleStyle);
        using var headerFont = CreateFont(_options.HeaderStyle);
        using var sigFont = CreateFont(_options.SignatureStyle);
        using var tsFont = CreateFont(_options.TimestampStyle);

        var sheet = Compose(thumbnails, body, title, titleFont, headerFont, sigFont, tsFont);

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

            case BlurShadow(var b, var blur, var radius):
                {
                    using var p = new SKPaint
                    {
                        Color = new SKColor(0, 0, 0, 140),
                        ImageFilter = SKImageFilter.CreateBlur(blur, blur),
                        IsAntialias = true,
                    };
                    canvas.DrawRoundRect(b, radius, radius, p);
                    break;
                }

            case RoundedClip(var b, var radius, var inner):
                {
                    canvas.Save();
                    using var clip = new SKRoundRect(b, radius);
                    canvas.ClipRoundRect(clip, SKClipOperation.Intersect, antialias: true);
                    foreach (var e in inner)
                    {
                        Draw(canvas, e);
                    }

                    canvas.Restore();
                    break;
                }

            case Border(var b, var radius, var width, var color):
                {
                    using var p = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width };

                    // Inset by half the stroke, so the whole border lies inside the thumbnail.
                    var inset = SKRect.Inflate(b, -width / 2f, -width / 2f);
                    float r = Math.Max(0, radius - (width / 2f));
                    canvas.DrawRoundRect(inset, r, r, p);
                    break;
                }

            case BitmapEl(var bmp, var dest):
                // Thumbnails are captured at display size, so this is a 1:1 copy; Nearest matches
                // the prior paintless DrawBitmap default (SkiaSharp 4.x deprecated that overload).
                canvas.DrawBitmap(bmp, dest, new SKSamplingOptions(SKFilterMode.Nearest));
                break;

            case TextEl(var text, var font, var color, var x, var y):
                {
                    using var p = new SKPaint { Color = color, IsAntialias = true };
                    canvas.DrawText(text, x, y, SKTextAlign.Left, font, p);
                    break;
                }
        }
    }

    private static SKBitmap SampleImage(IReadOnlyList<Thumbnail> thumbnails)
        => (thumbnails.FirstOrDefault(t => !t.IsHighlight) ?? thumbnails.FirstOrDefault())?.Image
           ?? throw new InvalidOperationException("No thumbnails to render.");

    private Sheet Compose(
        IReadOnlyList<Thumbnail> thumbnails,
        BodyLayout body,
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
        var spacing = LayoutSpacing.For(o);
        int padding = spacing.Gap;
        var sample = SampleImage(thumbnails);

        var header = o.ShowHeader ? (HeaderOverride ?? HeaderColumns.Empty) : HeaderColumns.Empty;
        int hdrRows = Math.Max(header.Left.Count, header.Right.Count);

        float titleH = string.IsNullOrEmpty(title) ? 0f : LineHeight(titleFont) + (2 * padding) + 8;
        float headerH = hdrRows > 0 ? (hdrRows * LineHeight(headerFont)) + (2 * padding) + 8 : 0f;
        float sigH = o.ShowSignature && !string.IsNullOrEmpty(o.Signature)
            ? LineHeight(sigFont) + (2 * padding) + 6
            : 0f;

        // Highlights are video frames, drawn at double size in a band of their own.
        var hlGrid = new GridSpec(
            ThumbW: sample.Width * 2,
            ThumbH: sample.Height * 2,
            CellW: (sample.Width * 2) + spacing.FrameWidth,
            CellH: (sample.Height * 2) + spacing.FrameHeight,
            Margin: spacing.Margin,
            Columns: columns);

        int hlRows = RowCount(highlights.Count, columns);
        float hlBandH = hlRows > 0
            ? (2 * hlGrid.Margin) + (hlRows * hlGrid.CellH) + ((hlRows - 1) * padding)
            : 0f;

        int width = body.Width;
        int height = (int)Math.Ceiling(titleH + headerH + hlBandH + body.Height + sigH);

        float titleY = 0f;
        float headerY = titleY + titleH;
        float hlY = headerY + headerH;
        float bodyY = hlY + hlBandH;
        float sigY = height - sigH;

        var elements = Enumerable.Empty<Element>()
            .Concat(TitleBand(title, titleFont, o, width, titleY, titleH))
            .Concat(HeaderBand(header, headerFont, o, width, padding, headerY, headerH))
            .Concat(HighlightBand(highlights, hlGrid, o, tsFont, spacing, width, hlY, hlBandH))
            .Concat(BodyBand(regular, body, o, tsFont, spacing, bodyY))
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
        GridSpec hlGrid,
        ContactSheetOptions o,
        SKFont tsFont,
        LayoutSpacing spacing,
        int width,
        float y,
        float h)
    {
        if (h == 0)
        {
            yield break;
        }

        yield return new ColorRect(new SKRect(0, y, width, y + h), o.HighlightBackground);

        int i = 0;
        for (int row = 0; i < items.Count; row++)
        {
            int itemsInRow = Math.Min(hlGrid.Columns, items.Count - i);
            float rowW = (itemsInRow * hlGrid.CellW) + ((itemsInRow - 1) * spacing.Gap);
            float startX = (width - rowW) / 2f;
            float rowY = y + hlGrid.Margin + (row * (hlGrid.CellH + spacing.Gap));

            for (int col = 0; col < itemsInRow; col++, i++)
            {
                float cellX = startX + (col * (hlGrid.CellW + spacing.Gap));
                var picture = SKRect.Create(cellX + spacing.FrameSide, rowY + spacing.FrameTop, hlGrid.ThumbW, hlGrid.ThumbH);
                foreach (var e in ThumbnailElements(items[i], picture, o, tsFont, spacing))
                {
                    yield return e;
                }
            }
        }
    }

    private static IEnumerable<Element> BodyBand(
        IReadOnlyList<Thumbnail> items,
        BodyLayout body,
        ContactSheetOptions o,
        SKFont tsFont,
        LayoutSpacing spacing,
        float y)
        => items
            .Zip(body.Pictures)
            .SelectMany(pair => ThumbnailElements(
                pair.First,
                SKRect.Create(pair.Second.Left, pair.Second.Top + y, pair.Second.Width, pair.Second.Height),
                o,
                tsFont,
                spacing));

    private static IEnumerable<Element> ThumbnailElements(
        Thumbnail thumb,
        SKRect picture,
        ContactSheetOptions o,
        SKFont tsFont,
        LayoutSpacing spacing)
    {
        int shadow = o.SoftShadow ? Math.Max(0, o.ShadowSize) : 0;
        float radius = Math.Max(0, o.CornerRadius);
        var card = new SKRect(
            picture.Left - spacing.FrameSide,
            picture.Top - spacing.FrameTop,
            picture.Right + spacing.FrameSide,
            picture.Bottom + spacing.FrameBottom);

        if (shadow > 0)
        {
            // The shadow follows the outermost shape: the square polaroid card, or the picture itself.
            var caster = o.Polaroid ? card : picture;
            caster.Offset(shadow / 2f, shadow / 2f);
            yield return new BlurShadow(caster, shadow / 2f, o.Polaroid ? 0 : radius);
        }

        if (o.Polaroid)
        {
            yield return new ColorRect(card, SKColors.White);
        }

        Element[] content =
        [
            new BitmapEl(thumb.Image, picture),
            .. o.Timestamp && !string.IsNullOrEmpty(thumb.Caption)
                ? CaptionElements(thumb.Caption, tsFont, o, picture.Left, picture.Top, picture.Width, picture.Height)
                : [],
        ];

        // The caption is clipped with the picture, so it never pokes out of a rounded corner.
        if (radius > 0)
        {
            yield return new RoundedClip(picture, radius, content);
        }
        else
        {
            foreach (var e in content)
            {
                yield return e;
            }
        }

        if (o.BorderWidth > 0)
        {
            yield return new Border(picture, radius, o.BorderWidth, o.BorderColor);
        }
    }

    private static IEnumerable<Element> CaptionElements(
        string text, SKFont font, ContactSheetOptions o, float imgX, float imgY, float w, float h)
    {
        float pad = 4f;

        // Captions can be arbitrarily long (image file names), so clip them to the thumbnail.
        text = Ellipsize(font, text, w - 8 - (2 * pad));
        float textW = MeasureTextWidth(font, text);
        float lh = LineHeight(font);
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

    /// <summary>Shortens <paramref name="text"/> with a trailing ellipsis until it fits
    /// <paramref name="maxWidth"/>; returns it unchanged when it already fits.</summary>
    private static string Ellipsize(SKFont font, string text, float maxWidth)
    {
        if (maxWidth <= 0 || MeasureTextWidth(font, text) <= maxWidth)
        {
            return text;
        }

        for (int length = text.Length - 1; length > 0; length--)
        {
            var candidate = string.Concat(text.AsSpan(0, length), "…");
            if (MeasureTextWidth(font, candidate) <= maxWidth)
            {
                return candidate;
            }
        }

        return "…";
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
