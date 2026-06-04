namespace VideoContactSheet;

public sealed class AudioStream
{
    public int Index { get; init; }

    public string? Codec { get; init; }

    public int Channels { get; init; }

    public string? ChannelLayout { get; init; }

    public int? SampleRate { get; init; }

    public long? BitRate { get; init; }
}
