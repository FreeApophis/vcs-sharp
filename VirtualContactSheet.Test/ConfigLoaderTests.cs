using VirtualContactSheet.Cli;

namespace VirtualContactSheet.Test;

public sealed class ConfigLoaderTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"vcs-config-{Guid.NewGuid():N}.toml");

    [Fact]
    public void Load_ExplicitFile_KeepsTheFilterSection()
    {
        File.WriteAllText(_path, """
            [main]
            padding = 0

            [filter]
            header = false
            """);

        var config = ConfigLoader.Load(_path);

        Assert.Equal(0, config.Main?.Padding);
        Assert.False(config.Filter?.Header);
    }

    [Fact]
    public void Load_ExplicitFile_KeepsTheImageLayout()
    {
        File.WriteAllText(_path, """
            [image]
            layout = "square"
            """);

        var config = ConfigLoader.Load(_path);

        Assert.Equal("square", config.Image?.Layout);
    }

    [Fact]
    public void Load_ExplicitFile_KeepsTheThumbnailStyle()
    {
        File.WriteAllText(_path, """
            [style.thumbnail]
            corner_radius = 12
            border_width  = 2
            border_color  = "#ff0000"
            """);

        var config = ConfigLoader.Load(_path);

        Assert.Equal(12, config.Style?.Thumbnail?.CornerRadius);
        Assert.Equal(2, config.Style?.Thumbnail?.BorderWidth);
        Assert.Equal("#ff0000", config.Style?.Thumbnail?.BorderColor);
    }

    public void Dispose() => File.Delete(_path);
}
