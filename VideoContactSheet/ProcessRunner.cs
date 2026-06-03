using System.Diagnostics;
using System.Text;

namespace VideoContactSheet;

internal static class ProcessRunner
{
    public sealed record Result(int ExitCode, string StdOut, string StdErr);

    public static async Task<Result> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in arguments)
            psi.ArgumentList.Add(a);

        using var process = new Process { StartInfo = psi };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new ToolNotFoundException(
                $"Could not start '{fileName}'. Make sure it is installed and on PATH.", ex);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct).ConfigureAwait(false);

        return new Result(process.ExitCode, stdout.ToString(), stderr.ToString());
    }
}

/// <summary>Thrown when an external tool (ffmpeg/ffprobe) cannot be located or launched.</summary>
public sealed class ToolNotFoundException : Exception
{
    public ToolNotFoundException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>Thrown when capture or probing fails.</summary>
public sealed class CaptureException : Exception
{
    public CaptureException(string message, Exception? inner = null) : base(message, inner) { }
}
