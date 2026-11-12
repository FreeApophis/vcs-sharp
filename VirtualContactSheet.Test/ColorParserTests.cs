using VirtualContactSheet.Cli;

namespace VirtualContactSheet.Test;

public class ColorParserTests
{
    [Theory]
    [InlineData("red", 0xFF, 0x00, 0x00, 0xFF)]
    [InlineData("  Navy ", 0x00, 0x00, 0x80, 0xFF)]
    [InlineData("TRANSPARENT", 0x00, 0x00, 0x00, 0x00)]
    [InlineData("#f80", 0xFF, 0x88, 0x00, 0xFF)]
    [InlineData("#f808", 0xFF, 0x88, 0x00, 0x88)]
    [InlineData("#1A2b3C", 0x1A, 0x2B, 0x3C, 0xFF)]
    [InlineData("#1a2b3c80", 0x1A, 0x2B, 0x3C, 0x80)]
    public void ValidColor_IsParsed(string value, byte red, byte green, byte blue, byte alpha)
    {
        Assert.Some(new SKColor(red, green, blue, alpha), ColorParser.Parse(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nocolor")]
    [InlineData("ff8800")]
    [InlineData("#")]
    [InlineData("#12")]
    [InlineData("#12345")]
    [InlineData("#123456789")]
    [InlineData("#ggg")]
    [InlineData("#12345g")]
    public void InvalidColor_IsRejected(string? value)
    {
        Assert.None(ColorParser.Parse(value));
    }
}
