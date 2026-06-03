namespace VideoContactSheet.Test;

public class TimeIndexTests
{
    [Theory]
    [InlineData("90", 90)]
    [InlineData("1:22", 82)]
    [InlineData("1:02:03", 3723)]
    [InlineData("3m30", 210)]
    [InlineData("3m30s", 210)]
    [InlineData("1h2m3s", 3723)]
    [InlineData("500ms", 0.5)]
    [InlineData(" 45 ", 45)]
    public void Parse_AcceptsFlexibleFormats(string input, double expectedSeconds)
    {
        var parsed = TimeIndex.Parse(input);

        Assert.Equal(expectedSeconds, parsed.TotalSeconds, precision: 3);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("1:2:3:4")]
    public void TryParse_RejectsInvalidInput(string input)
    {
        var ok = TimeIndex.TryParse(input, out _);

        Assert.False(ok);
    }

    [Theory]
    [InlineData(82, "01:22")]
    [InlineData(3723, "1:02:03")]
    [InlineData(5, "00:05")]
    public void ToTimestamp_FormatsWithHoursOnlyWhenPresent(double seconds, string expected)
    {
        var formatted = new TimeIndex(seconds).ToTimestamp();

        Assert.Equal(expected, formatted);
    }
}
