using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.Releases;
using Ritten.Releases.Steps;
using Ritten.Reporting;

namespace Ritten.Tests.Releases;

/// <summary>
/// What a pull request changed of what ships: each shipped project's directory and the shared build inputs,
/// measured from the merge base so the base branch's own progress is never counted as the branch's — and
/// measured only when the release cadence judges it.
/// </summary>
public class ReadShippedChangesTests
{
    private readonly IGit _git = Substitute.For<IGit>();

    public ReadShippedChangesTests() =>
        _git.ChangedFilesSince(Arg.Any<IDirectory>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);

    [Fact]
    public async Task IsUnreviewedOutsideAPullRequest()
    {
        var changes = await Produce(new PullRequest(), "src/My.Tool/My.Tool.csproj");

        changes.Reviewed.ShouldBeFalse();
        changes.Any.ShouldBeFalse();
        await _git.DidNotReceiveWithAnyArgs().ChangedFilesSince(Arg.Any<IDirectory>(), default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IsUnreviewedWhenTheCadenceDoesNotJudgeTheChanges()
    {
        // A curated release never reads the diff, so its pull requests never pay for the fetch, or fail on it.
        var changes = await Produce(Reviewing("main"), ReleaseCadence.Curated, "src/My.Tool/My.Tool.csproj");

        changes.Reviewed.ShouldBeFalse();
        await _git.DidNotReceiveWithAnyArgs().FetchMergeBase(Arg.Any<IDirectory>(), default!, default!, TestContext.Current.CancellationToken);
        await _git.DidNotReceiveWithAnyArgs().ChangedFilesSince(Arg.Any<IDirectory>(), default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FetchesTheBaseBeforeDiffing()
    {
        var fetched = false;
        var diffedFirst = false;
        _git.When(g => g.FetchMergeBase(Arg.Any<IDirectory>(), "origin", "main", Arg.Any<CancellationToken>())).Do(_ => fetched = true);
        _git.When(g => g.ChangedFilesSince(Arg.Any<IDirectory>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())).Do(_ => diffedFirst |= !fetched);

        await Produce(Reviewing("main"), "src/My.Tool/My.Tool.csproj");

        // A CI checkout has never fetched the base; diffing first would fail on the missing reference.
        fetched.ShouldBeTrue();
        diffedFirst.ShouldBeFalse();
    }

    [Fact]
    public async Task DiffsEachShippedProjectAndTheSharedInputsAgainstTheRemoteBase()
    {
        await Produce(Reviewing("main"), "src/My.Tool/My.Tool.csproj", "src/My.Core/My.Core.csproj");

        foreach (var path in new[] { "src/My.Tool", "src/My.Core", "Directory.Build.props", "Directory.Packages.props" })
        {
            await _git.Received(1).ChangedFilesSince(Arg.Any<IDirectory>(), "origin/main", path, Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task ReportsWhatChanged()
    {
        _git.ChangedFilesSince(Arg.Any<IDirectory>(), "origin/main", "src/My.Tool", Arg.Any<CancellationToken>()).Returns(["src/My.Tool/Program.cs"]);
        _git.ChangedFilesSince(Arg.Any<IDirectory>(), "origin/main", "Directory.Packages.props", Arg.Any<CancellationToken>()).Returns(["Directory.Packages.props"]);

        var changes = await Produce(Reviewing("main"), "src/My.Tool/My.Tool.csproj");

        changes.Reviewed.ShouldBeTrue();
        changes.BaseRef.ShouldBe("main");
        changes.Files.ShouldBe(["src/My.Tool/Program.cs", "Directory.Packages.props"], ignoreOrder: true);
    }

    [Fact]
    public async Task IsEmptyWhenOnlyUnshippedFilesChanged()
    {
        var changes = await Produce(Reviewing("main"), "src/My.Tool/My.Tool.csproj");

        changes.Reviewed.ShouldBeTrue();
        changes.Any.ShouldBeFalse();
    }

    [Fact]
    public async Task TreatsAProjectAtTheRootAsTheWholeRepository()
    {
        await Produce(Reviewing("main"), "My.Tool.csproj");

        await _git.Received(1).ChangedFilesSince(Arg.Any<IDirectory>(), "origin/main", ".", Arg.Any<CancellationToken>());
    }

    private static PullRequest Reviewing(string baseRef) => new() { Number = 7, BaseRef = baseRef };

    private Task<ShippedChanges> Produce(PullRequest pullRequest, params string[] projectFiles) =>
        Produce(pullRequest, ReleaseCadence.Continuous, projectFiles);

    private async Task<ShippedChanges> Produce(PullRequest pullRequest, ReleaseCadence cadence, params string[] projectFiles)
    {
        var packages = new PackageSet
        {
            Packages = [.. projectFiles.Select(file => new Project { Name = Path.GetFileNameWithoutExtension(file), Version = NuGetVersion.Parse("1.0.0"), ProjectFile = file })]
        };

        var release = new ReleaseSettings { Cadence = cadence };
        var result = await new ReadShippedChanges(pullRequest, _git, Substitute.For<IFileSystem>(), Substitute.For<IWorkflowLog>()).Run(release, packages, TestContext.Current.CancellationToken);
        return result.Value.ShouldNotBeNull();
    }
}
