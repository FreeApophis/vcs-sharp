namespace VideoContactSheet.Cli;

internal sealed class VcsConfig
{
    public MainConfig? Main { get; set; }

    public FilterConfig? Filter { get; set; }

    public StyleConfig? Style { get; set; }
}

internal sealed class MainConfig
{
    public int? Columns { get; set; }

    public int? Rows { get; set; }

    public int? Padding { get; set; }

    public int? Quality { get; set; }

    public int? Width { get; set; }

    public string? Interval { get; set; }
}

internal sealed class FilterConfig
{
    public bool? Timestamp { get; set; }

    public bool? Polaroid { get; set; }

    public bool? Shadow { get; set; }

    public bool? BlankEvasion { get; set; }

    public double? BlankThreshold { get; set; }
}

internal sealed class StyleConfig
{
    public TextStyleConfig? Header { get; set; }

    public TextStyleConfig? Title { get; set; }

    public TextStyleConfig? Timestamp { get; set; }

    public TextStyleConfig? Signature { get; set; }

    public BackgroundConfig? Contact { get; set; }

    public BackgroundConfig? Highlight { get; set; }
}

internal sealed class TextStyleConfig
{
    public float? Size { get; set; }

    public string? Color { get; set; }

    public string? Background { get; set; }

    public string? FontFamily { get; set; }

    public string? FontFile { get; set; }

    public bool? Bold { get; set; }
}

internal sealed class BackgroundConfig
{
    public string? Background { get; set; }
}
