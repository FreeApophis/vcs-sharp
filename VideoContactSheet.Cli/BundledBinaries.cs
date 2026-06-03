namespace VideoContactSheet.Cli;

/// <summary>Locates the ffmpeg/ffprobe binaries copied next to the CLI at build time.</summary>
internal static class BundledBinaries
{
    /// <summary>
    /// Returns the application base directory when a bundled ffmpeg binary is present there,
    /// otherwise <c>null</c> (so the core library falls back to PATH).
    /// </summary>
    public static string? Detect()
    {
        var dir = AppContext.BaseDirectory;
        var name = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        return File.Exists(Path.Combine(dir, name)) ? dir : null;
    }
}
