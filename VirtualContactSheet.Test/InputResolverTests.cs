using VirtualContactSheet.Cli;

namespace VirtualContactSheet.Test;

public class InputResolverTests
{
    [Fact]
    public void VideoFile_ResolvesToOneVideoSheet()
    {
        var inputs = Resolve(["movie.mkv"]);

        var input = Assert.Single(inputs);
        Assert.Equal(SheetInputKind.Video, input.Kind);
        Assert.Equal(["movie.mkv"], input.Paths);
        Assert.Equal("movie", input.OutputBase);
    }

    [Fact]
    public void SeveralVideos_StayOneSheetEach()
    {
        var inputs = Resolve(["a.mkv", "b.mp4"]);

        Assert.Equal(2, inputs.Count);
        Assert.All(inputs, input => Assert.Equal(SheetInputKind.Video, input.Kind));
    }

    [Fact]
    public void UnknownExtension_FallsBackToVideo()
    {
        // Anything the image side does not claim must keep resolving to video, so unusual
        // containers work exactly as they did before image support existed.
        var input = Assert.Single(Resolve(["recording.mts"]));

        Assert.Equal(SheetInputKind.Video, input.Kind);
    }

    [Fact]
    public void ExtensionlessFile_FallsBackToVideo()
    {
        var input = Assert.Single(Resolve(["dump"]));

        Assert.Equal(SheetInputKind.Video, input.Kind);
        Assert.Equal("dump", input.OutputBase);
    }

    [Fact]
    public void Folder_ResolvesToOneImageSheetNamedAfterIt()
    {
        using var folder = new TempImageFolder();
        folder.Add("a.png");

        var input = Assert.Single(Resolve([folder.Root]));

        Assert.Equal(SheetInputKind.Images, input.Kind);
        Assert.Equal(folder.Root, input.Folder);
        Assert.Equal(folder.Root, input.OutputBase);
    }

    [Fact]
    public void FolderWithTrailingSeparator_DoesNotLeakItIntoTheOutputName()
    {
        using var folder = new TempImageFolder();
        folder.Add("a.png");

        var input = Assert.Single(Resolve([folder.Root + Path.DirectorySeparatorChar]));

        Assert.Equal(folder.Root, input.OutputBase);
    }

    [Fact]
    public void LooseImageFiles_CoalesceIntoASingleSheet()
    {
        var inputs = Resolve(["a.jpg", "b.png", "c.webp"]);

        var input = Assert.Single(inputs);
        Assert.Equal(SheetInputKind.Images, input.Kind);
        Assert.Equal(["a.jpg", "b.png", "c.webp"], input.Paths);
        Assert.Null(input.Folder);
        Assert.Equal("contact-sheet", input.OutputBase);
    }

    [Fact]
    public void SingleLooseImage_IsNamedAfterItself()
    {
        var input = Assert.Single(Resolve(["holiday/beach.jpg"]));

        Assert.Equal(SheetInputKind.Images, input.Kind);
        Assert.Equal("holiday/beach", input.OutputBase);
    }

    [Fact]
    public void MixedInputs_KeepVideosSeparateAndGroupImagesAtTheFirstOne()
    {
        var inputs = Resolve(["a.mkv", "one.jpg", "b.mkv", "two.jpg"]);

        Assert.Equal(3, inputs.Count);
        Assert.Equal(SheetInputKind.Video, inputs[0].Kind);

        // The group takes the slot of the first loose image, ahead of the second video.
        Assert.Equal(SheetInputKind.Images, inputs[1].Kind);
        Assert.Equal(["one.jpg", "two.jpg"], inputs[1].Paths);

        Assert.Equal(SheetInputKind.Video, inputs[2].Kind);
        Assert.Equal("b", inputs[2].OutputBase);
    }

    [Fact]
    public void FolderAndLooseImages_StayDistinctSheets()
    {
        using var folder = new TempImageFolder();
        folder.Add("a.png");

        var inputs = Resolve([folder.Root, "loose.jpg"]);

        Assert.Equal(2, inputs.Count);
        Assert.Equal(folder.Root, inputs[0].Folder);
        Assert.Null(inputs[1].Folder);
    }

    [Fact]
    public void Wildcard_ExpandsToTheMatchingFiles()
    {
        using var folder = new TempImageFolder();
        folder.Add("b.png");
        folder.Add("a.png");
        folder.AddNonImage("notes.txt");

        var input = Assert.Single(Resolve([Path.Combine(folder.Root, "*.png")]));

        Assert.Equal(SheetInputKind.Images, input.Kind);
        Assert.Equal(["a.png", "b.png"], input.Paths.Select(Path.GetFileName));
    }

    [Fact]
    public void Wildcard_MatchingNothing_IsAnError()
    {
        using var folder = new TempImageFolder();

        var ok = InputResolver.TryResolve([Path.Combine(folder.Root, "*.png")], out _, out var error);

        Assert.False(ok);
        Assert.Contains("No files matched", error);
    }

    [Fact]
    public void Wildcard_InAMissingFolder_IsAnError()
    {
        var ok = InputResolver.TryResolve(
            [Path.Combine(Path.GetTempPath(), "vcs-no-such-dir-7c1a", "*.png")], out _, out var error);

        Assert.False(ok);
        Assert.Contains("No files matched", error);
    }

    private static IReadOnlyList<SheetInput> Resolve(string[] arguments)
    {
        Assert.True(InputResolver.TryResolve(arguments, out var inputs, out var error), error);
        return inputs;
    }
}
