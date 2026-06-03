using System.Globalization;
using System.Text.RegularExpressions;

namespace VideoContactSheet;

/// <summary>
/// Represents a point in time within a video. Parses the flexible time formats
/// supported by the original vcs script: plain seconds ("90"), colon notation
/// ("1:22", "1:02:03") and unit notation ("3m30", "1h2m3s", "500ms").
/// </summary>
public readonly struct TimeIndex : IComparable<TimeIndex>, IEquatable<TimeIndex>
{
    /// <summary>Total time as a <see cref="TimeSpan"/>.</summary>
    public TimeSpan Value { get; }

    public double TotalSeconds => Value.TotalSeconds;

    public TimeIndex(TimeSpan value) => Value = value;

    public TimeIndex(double seconds) => Value = TimeSpan.FromSeconds(seconds);

    private static readonly Regex UnitPattern = new(
        @"^\s*(?:(?<h>\d+(?:\.\d+)?)h)?(?:(?<m>\d+(?:\.\d+)?)m(?!s))?(?:(?<s>\d+(?:\.\d+)?)s)?(?:(?<ms>\d+(?:\.\d+)?)ms)?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Parse a flexible time string. Accepts:
    /// "90" (seconds), "1:22" (m:s), "1:02:03" (h:m:s),
    /// "3m30" / "3m30s" / "1h2m3s" / "500ms" (unit form).
    /// </summary>
    public static TimeIndex Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new FormatException("Time index is empty.");

        input = input.Trim();

        // Colon notation: [h:]m:s(.frac)
        if (input.Contains(':'))
        {
            var parts = input.Split(':');
            if (parts.Length is < 2 or > 3)
                throw new FormatException($"Invalid colon time index: '{input}'.");

            double seconds = 0;
            foreach (var p in parts)
                seconds = seconds * 60 + double.Parse(p, CultureInfo.InvariantCulture);

            return new TimeIndex(seconds);
        }

        // Plain seconds
        if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var plain))
            return new TimeIndex(plain);

        // Unit notation (allow "3m30" meaning 3m30s)
        var normalized = Regex.Replace(input, @"(\dm)(\d+)$", "$1$2s", RegexOptions.IgnoreCase);
        var match = UnitPattern.Match(normalized);
        if (!match.Success || normalized.Length == 0)
            throw new FormatException($"Invalid time index: '{input}'.");

        double total = 0;
        if (match.Groups["h"].Success) total += double.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture) * 3600;
        if (match.Groups["m"].Success) total += double.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture) * 60;
        if (match.Groups["s"].Success) total += double.Parse(match.Groups["s"].Value, CultureInfo.InvariantCulture);
        if (match.Groups["ms"].Success) total += double.Parse(match.Groups["ms"].Value, CultureInfo.InvariantCulture) / 1000.0;

        if (total == 0 && !match.Groups["s"].Success && !match.Groups["m"].Success
            && !match.Groups["h"].Success && !match.Groups["ms"].Success)
            throw new FormatException($"Invalid time index: '{input}'.");

        return new TimeIndex(total);
    }

    public static bool TryParse(string input, out TimeIndex result)
    {
        try { result = Parse(input); return true; }
        catch { result = default; return false; }
    }

    /// <summary>Format as H:MM:SS (hours omitted when zero), matching the timestamp overlay style.</summary>
    public string ToTimestamp()
    {
        var ts = Value;
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    public int CompareTo(TimeIndex other) => Value.CompareTo(other.Value);
    public bool Equals(TimeIndex other) => Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is TimeIndex t && Equals(t);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => ToTimestamp();

    public static bool operator <(TimeIndex a, TimeIndex b) => a.CompareTo(b) < 0;
    public static bool operator >(TimeIndex a, TimeIndex b) => a.CompareTo(b) > 0;
    public static bool operator <=(TimeIndex a, TimeIndex b) => a.CompareTo(b) <= 0;
    public static bool operator >=(TimeIndex a, TimeIndex b) => a.CompareTo(b) >= 0;
    public static bool operator ==(TimeIndex a, TimeIndex b) => a.Equals(b);
    public static bool operator !=(TimeIndex a, TimeIndex b) => !a.Equals(b);
}
