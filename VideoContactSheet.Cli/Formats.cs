namespace VideoContactSheet.Cli;

/// <summary>Maps the <c>--format</c> string onto <see cref="SheetFormat"/> and file extensions.</summary>
internal static class Formats
{
    /// <summary>Format names accepted by <c>--format</c>.</summary>
    public static readonly string[] Accepted = ["png", "jpg", "jpeg", "webp"];

    public static SheetFormat Parse(string s) => s.ToLowerInvariant() switch
    {
        "jpg" or "jpeg" => SheetFormat.Jpg,
        "webp" => SheetFormat.Webp,
        _ => SheetFormat.Png,
    };

    public static string ExtensionFor(SheetFormat f) => f switch
    {
        SheetFormat.Jpg => "jpg",
        SheetFormat.Webp => "webp",
        _ => "png",
    };
}
