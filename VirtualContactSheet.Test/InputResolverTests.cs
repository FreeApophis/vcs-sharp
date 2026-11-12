using VirtualContactSheet.Cli;

namespace VirtualContactSheet.Test;

/// <summary>
/// Resolves real files: inputs are classified by what exists on disk, so every path a test
/// resolves is created first in a throwaway folder.
/// </summary>
public sealed class InputResolverTests : IDisposable
{
    private readonly TempImageFolder _files = new();

    public void Dispose() => _files.Dispose();

    [Fact]
    public void VideoFile_ResolvesToOneVideoSheet()
    {
        var movie = Video("movie.mkv");

        var input = Assert.IsType<VideoInput>(Assert.Single(Resolve([movie])));

        Assert.Equal(movie, input.Path);
        Assert.Equal(Named("movie"), input.OutputBase);
    }

    [Fact]
    public void SeveralVideos_StayOneSheetEach()
    {
        var inputs = Resolve([Video("a.mkv"), Video("b.mp4")]);

        Assert.Equal(2, inputs.Count);
        Assert.All(inputs, input => Assert.IsType<VideoInput>(input));
    }

    [Fact]
    public void UnknownExtension_FallsBackToVideo()
    {
        // Anything the image side does not claim must keep resolving to video, so unusual
        // containers work exactly as they did before image support existed.
        var input = Assert.Single(Resolve([Video("recording.mts")]));

        Assert.IsType<VideoInput>(input);
    }

    [Fact]
    public void ExtensionlessFile_FallsBackToVideo()
    {
        var input = Assert.IsType<VideoInput>(Assert.Single(Resolve([Video("dump")])));

        Assert.Equal(Named("dump"), input.OutputBase);
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
        string[] images = [Image("a.jpg"), Image("b.png"), Image("c.webp")];

        var input = Assert.IsType<LooseImagesInput>(Assert.Single(Resolve(images)));

        Assert.Equal(images, input.Paths);
        Assert.Equal("contact-sheet", input.OutputBase);
    }

    [Fact]
    public void SingleLooseImage_IsNamedAfterItself()
    {
        var beach = Image(Path.Combine("holiday", "beach.jpg"));

        var input = Assert.IsType<LooseImagesInput>(Assert.Single(Resolve([beach])));

        Assert.Equal(Named(Path.Combine("holiday", "beach")), input.OutputBase);
    }

    [Fact]
    public void MixedInputs_KeepVideosSeparateAndGroupImagesAtTheFirstOne()
    {
        string[] arguments = [Video("a.mkv"), Image("one.jpg"), Video("b.mkv"), Image("two.jpg")];

        var inputs = Resolve(arguments);

        Assert.Equal(3, inputs.Count);
        Assert.IsType<VideoInput>(inputs[0]);

        // The group takes the slot of the first loose image, ahead of the second video.
        Assert.Equal([arguments[1], arguments[3]], Assert.IsType<LooseImagesInput>(inputs[1]).Paths);

        Assert.Equal(Named("b"), Assert.IsType<VideoInput>(inputs[2]).OutputBase);
    }

    [Fact]
    public void FolderAndLooseImages_StayDistinctSheets()
    {
        using var folder = new TempImageFolder();
        folder.Add("a.png");
        var loose = Image("loose.jpg");

        var inputs = Resolve([folder.Root, loose]);

        Assert.Equal(2, inputs.Count);
        Assert.Equal(folder.Root, Assert.IsType<ImageFolderInput>(inputs[0]).Folder);
        Assert.Equal([loose], Assert.IsType<LooseImagesInput>(inputs[1]).Paths);
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

        var error = Assert.Left(InputResolver.Resolve([Video("movie.mkv"), first, second]));

        Assert.Equal($"No files matched '{first}'.", error);
    }

    [Fact]
    public void MissingFolder_IsAnError()
    {
        var missing = Named("holiday") + Path.DirectorySeparatorChar;

        var error = Assert.Left(InputResolver.Resolve([missing]));

        Assert.Equal($"Folder not found: '{missing}'.", error);
    }

    [Theory]
    [InlineData("nope.mp4")]
    [InlineData("gone.jpg")]
    [InlineData("holiday")]
    public void MissingFileOrFolder_IsAnError(string name)
    {
        // Without a trailing separator a missing path could have been either.
        var missing = Named(name);

        var error = Assert.Left(InputResolver.Resolve([missing]));

        Assert.Equal($"File or folder not found: '{missing}'.", error);
    }

    [Fact]
    public void MissingPath_RejectsTheWholeRun()
    {
        // Reported before any sheet is rendered, so a typo cannot waste a long run.
        var missing = Named("typo.mkv");

        var error = Assert.Left(InputResolver.Resolve([Video("a.mkv"), missing, Image("b.png")]));

        Assert.Equal($"File or folder not found: '{missing}'.", error);
    }

    [Fact]
    public void SeveralMissingPaths_ReportTheFirstOne()
    {
        var first = Named("first.mkv");

        var error = Assert.Left(InputResolver.Resolve([first, Named("second.mkv")]));

        Assert.Equal($"File or folder not found: '{first}'.", error);
    }

    private static IReadOnlyList<SheetInput> Resolve(string[] arguments)
        => Assert.Right(InputResolver.Resolve(arguments));

    /// <summary>A file the resolver must treat as a video; its content is never read.</summary>
    private string Video(string name) => _files.AddNonImage(name);

    private string Image(string name) => _files.Add(name);

    /// <summary>The expected output base: a path in the test folder, without extension.</summary>
    private string Named(string name) => Path.Combine(_files.Root, name);
}
