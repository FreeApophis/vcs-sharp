namespace VideoContactSheet.Test;

public class ComputeTimesTests
{
    [Fact]
    public void GridMode_ProducesColumnsTimesRowsTimestampsWithinDuration()
    {
        var options = new ContactSheetOptions { Columns = 4, Rows = 3 };

        var times = NewVideo().ComputeTimes(HundredSeconds, options);

        Assert.Equal(12, times.Count);
        Assert.All(times, t => Assert.InRange(t.TotalSeconds, 0, 100));
        Assert.True(times.Zip(times.Skip(1)).All(pair => pair.First < pair.Second), "times should be strictly increasing");
    }

    [Fact]
    public void IntervalMode_StepsByIntervalAndIgnoresRows()
    {
        var options = new ContactSheetOptions { Rows = 99, Interval = new TimeIndex(25) };

        var times = NewVideo().ComputeTimes(HundredSeconds, options);

        Assert.Equal([0d, 25, 50, 75], times.Select(t => t.TotalSeconds));
    }

    [Fact]
    public void Range_RestrictsSamplingToFromTo()
    {
        var options = new ContactSheetOptions { Columns = 2, Rows = 1, From = new TimeIndex(40), To = new TimeIndex(60) };

        var times = NewVideo().ComputeTimes(HundredSeconds, options);

        Assert.All(times, t => Assert.InRange(t.TotalSeconds, 40, 60));
    }

    [Fact]
    public Task GridMode_DefaultLayout_MatchesSnapshot()
    {
        var times = NewVideo().ComputeTimes(HundredSeconds, new ContactSheetOptions { Columns = 4, Rows = 4 });

        return Verify(times.Select(t => t.ToTimestamp()));
    }

    private static readonly VideoInfo HundredSeconds = new()
    {
        Duration = TimeSpan.FromSeconds(100),
        VideoStreams = [new VideoStream { Index = 0, Width = 1920, Height = 1080 }],
    };

    private static Video NewVideo() => new("dummy.mkv", new FakeCapturer(), new FakeProbe());
}
