namespace VideoContactSheet;

/// <summary>Thrown when a frame capture attempt fails.</summary>
public sealed class CaptureException : Exception
{
    public CaptureException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}
