using VirtualContactSheet.Cli;

namespace VirtualContactSheet.Test;

public class InputResolverTests
{
    [Fact]
    public void VideoFile_ResolvesToOneVideoSheet()
    {
        var inputs = Resolve(["movie.mkv"]);

        var input = Assert.IsType<VideoInput>(Assert.Single(inputs));
        Assert.Equal("movie.mkv", input.Path);
        Assert.Equal("movie", input.OutputBase);
    }

    [Fact]
    public void SeveralVideos_StayOneSheetEach()
    {
        var inputs = Resolve(["a.mkv", "b.mp4"]);

        Assert.Equal(2, inputs.Count);
        Assert.All(inputs, input => Assert.IsType<VideoInput>(input));
    }

    [Fact]
    public void UnknownExtension_FallsBackToVideo()
    {
        // Anything the image side does not claim must keep resolving to video, so unusual
        // containers work exactly as they did before image support existed.
        var input = Assert.Single(Resolve(["recording.mts"]));

        Assert.IsType<VideoInput>(input);
    }

    [Fact]
    public void ExtensionlessFile_FallsBackToVideo()
    {
        var input = Assert.IsType<VideoInput>(Assert.Single(Resolve(["dump"])));

        Assert.Equal("dump", input.OutputBase);
    }

    [Fact]
    public void Folder_ResolvesToOneImageSheetNamedAfterIt()
    {
        using var folder = new TempImageFolder();
        folder.Add("a.png");

        var input = Assert.IsType<ImageFolderInput>(Assert.Single(Resolve([folder.Root])));

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

        var input = Assert.IsType<LooseImagesInput>(Assert.Single(inputs));
        Assert.Equal(["a.jpg", "b.png", "c.webp"], input.Paths);
        Assert.Equal("contact-sheet", input.OutputBase);
    }

    [Fact]
    public void SingleLooseImage_IsNamedAfterItself()
    {
        var input = Assert.IsType<LooseImagesInput>(Assert.Single(Resolve(["holiday/beach.jpg"])));

        Assert.Equal("holiday/beach", input.OutputBase);
    }

    [Fact]
    public void MixedInputs_KeepVideosSeparateAndGroupImagesAtTheFirstOne()
    {
        var inputs = Resolve(["a.mkv", "one.jpg", "b.mkv", "two.jpg"]);

        Assert.Equal(3, inputs.Count);
        Assert.IsType<VideoInput>(inputs[0]);

        // The group takes the slot of the first loose image, ahead of the second video.
        Assert.Equal(["one.jpg", "two.jpg"], Assert.IsType<LooseImagesInput>(inputs[1]).Paths);

        Assert.Equal("b", Assert.IsType<VideoInput>(inputs[2]).OutputBase);
    }

    [Fact]
    public void FolderAndLooseImages_StayDistinctSheets()
    {
        using var folder = new TempImageFolder();
        folder.Add("a.png");

        var inputs = Resolve([folder.Root, "loose.jpg"]);

        Assert.Equal(2, inputs.Count);
        Assert.Equal(folder.Root, Assert.IsType<ImageFolderInput>(inputs[0]).Folder);
        Assert.Equal(["loose.jpg"], Assert.IsType<LooseImagesInput>(inputs[1]).Paths);
    }

    [Fact]
    public void Wildcard_ExpandsToTheMatchingFiles()
    {
        using var folder = new TempImageFolder();
        folder.Add("b.png");
        folder.Add("a.png");
        folder.AddNonImage("notes.txt");

        var input = Assert.IsType<LooseImagesInput>(Assert.Single(Resolve([Path.Combine(folder.Root, "*.png")])));

        Assert.Equal(["a.png", "b.png"], input.Paths.Select(Path.GetFileName));
    }

    [Fact]
    public void Wildcard_MatchingNothing_IsAnError()
    {
        using var folder = new TempImageFolder();

        var error = Assert.Left(InputResolver.Resolve([Path.Combine(folder.Root, "*.png")]));

        Assert.Contains("No files matched", error);
    }

    [Fact]
    public void Wildcard_InAMissingFolder_IsAnError()
    {
        var error = Assert.Left(InputResolver.Resolve(
            [Path.Combine(Path.GetTempPath(), "vcs-no-such-dir-7c1a", "*.png")]));

        Assert.Contains("No files matched", error);
    }

    [Fact]
    public void SeveralFailingArguments_ReportTheFirstOne()
    {
        using var folder = new TempImageFolder();
        var first = Path.Combine(folder.Root, "*.png");
        var second = Path.Combine(folder.Root, "*.jpg");

        var error = Assert.Left(InputResolver.Resolve(["movie.mkv", first, second]));

        Assert.Equal($"No files matched '{first}'.", error);
    }

    private static IReadOnlyList<SheetInput> Resolve(string[] arguments)
        => Assert.Right(InputResolver.Resolve(arguments));
}
