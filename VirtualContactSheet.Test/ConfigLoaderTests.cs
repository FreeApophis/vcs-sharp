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

    public void Dispose() => File.Delete(_path);
}
