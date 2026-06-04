using SkiaSharp;

namespace VideoContactSheet;

/// <summary>
/// Provides the DejaVu Sans typefaces embedded in this assembly, so text renders identically
/// on every platform without relying on installed system fonts. Loaded once and cached.
/// </summary>
internal static class BundledFonts
{
    private static readonly Lazy<SKTypeface> LazyRegular = new(() => Load("DejaVuSans.ttf"));
    private static readonly Lazy<SKTypeface> LazyBold = new(() => Load("DejaVuSans-Bold.ttf"));

    public static SKTypeface Regular => LazyRegular.Value;

    public static SKTypeface Bold => LazyBold.Value;

    public static SKTypeface ForWeight(bool bold) => bold ? Bold : Regular;

    private static SKTypeface Load(string fileName)
    {
        var assembly = typeof(BundledFonts).Assembly;
        var resourceName = $"VideoContactSheet.Fonts.{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded font resource '{resourceName}' was not found.");

        // SKTypeface.FromStream copies the data, so disposing the stream afterwards is safe.
        return SKTypeface.FromStream(stream)
            ?? throw new InvalidOperationException($"Failed to load embedded font '{resourceName}'.");
    }
}
