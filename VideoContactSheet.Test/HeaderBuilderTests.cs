namespace VideoContactSheet.Test;

public class HeaderBuilderTests
{
    [Fact]
    public void Build_LeftColumn_HasFilenameSizeAndLength()
    {
        var header = HeaderBuilder.Build("movie.mp4", SampleInfo());

        Assert.Equal(
            ["Filename: movie.mp4", "File size: 600 MiB", "Length: 14:34"],
            header.Left);
    }

    [Fact]
    public void Build_RightColumn_HasDimensionsFormatAndFps()
    {
        var header = HeaderBuilder.Build("movie.mp4", SampleInfo());

        Assert.Equal(
            ["Dimensions: 1280x720", "Format: h264 (High) / aac", "FPS: 29.97"],
            header.Right);
    }

    [Fact]
    public void Build_OmitsProfileWhenAbsentAndAudioWhenMissing()
    {
        var info = new VideoInfo
        {
            FileSize = 1024,
            Duration = TimeSpan.FromMinutes(1),
            VideoStreams = [new VideoStream { Width = 640, Height = 480, Codec = "vp9", FrameRate = 24 }],
            AudioStreams = Array.Empty<AudioStream>(),
        };

        var header = HeaderBuilder.Build("clip.webm", info);

        Assert.Contains("Format: vp9", header.Right);
        Assert.DoesNotContain(header.Right, line => line.Contains('/'));
    }

    [Fact]
    public void Build_LongVideo_FormatsLengthWithHours()
    {
        var info = new VideoInfo
        {
            FileSize = 1024,
            Duration = new TimeSpan(1, 2, 3),
            VideoStreams = [new VideoStream { Width = 1280, Height = 720, Codec = "h264" }],
        };

        var header = HeaderBuilder.Build("movie.mp4", info);

        Assert.Contains("Length: 1:02:03", header.Left);
    }

    private static VideoInfo SampleInfo() => new()
    {
        FileSize = 600L * 1024 * 1024,
        Duration = new TimeSpan(0, 14, 34),
        VideoStreams =
        [
            new VideoStream { Width = 1280, Height = 720, Codec = "h264", Profile = "High", FrameRate = 29.97 },
        ],
        AudioStreams = [new AudioStream { Codec = "aac", SampleRate = 44100, Channels = 2 }],
    };
}
