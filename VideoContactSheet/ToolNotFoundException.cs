namespace VideoContactSheet;

/// <summary>Thrown when an external tool (ffmpeg/ffprobe) cannot be located or launched.</summary>
public sealed class ToolNotFoundException : Exception
{
    public ToolNotFoundException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

/// <summary>Thrown when capture or probing fails.</summary>
public sealed class CaptureException : Exception
{
    public CaptureException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}
