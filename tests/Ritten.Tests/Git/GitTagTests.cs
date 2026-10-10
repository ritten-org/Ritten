using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.Git.Steps;
using Ritten.Releases;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Git;

public class GitTagTests
{
    private static readonly Project TheProject = new() { Name = "My.Package", Version = NuGetVersion.Parse("1.2.0") };

    private readonly IGit _git = Substitute.For<IGit>();
    private readonly GitOptions _options = TestOptions.Git();
    private ReleaseSettings _release = TestOptions.Release();

    [Fact]
    public async Task SkipsWhenTheTagAlreadyExistsOnOrigin()
    {
        _git.RemoteTagExists(Arg.Any<IDirectory>(), "origin", "v1.2.0", Arg.Any<CancellationToken>()).Returns(true);

        await Step().Run(_release, TheProject, TestContext.Current.CancellationToken);

        await _git.DidNotReceiveWithAnyArgs().CreateTag(Arg.Any<IDirectory>(), default!, default, TestContext.Current.CancellationToken);
        await _git.DidNotReceiveWithAnyArgs().PushTag(Arg.Any<IDirectory>(), default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreatesAndPushesTheTagWhenItDoesNotExist()
    {
        await Step().Run(_release, TheProject, TestContext.Current.CancellationToken);

        await _git.Received().CreateTag(Arg.Any<IDirectory>(), "v1.2.0", null, Arg.Any<CancellationToken>());
        await _git.Received().PushTag(Arg.Any<IDirectory>(), "origin", "v1.2.0", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TagsTheConfiguredCommitSha()
    {
        _options.CommitSha = "abc123";

        await Step().Run(_release, TheProject, TestContext.Current.CancellationToken);

        await _git.Received().CreateTag(Arg.Any<IDirectory>(), "v1.2.0", "abc123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushesAnExistingLocalTagWithoutRecreatingIt()
    {
        _git.TagExists(Arg.Any<IDirectory>(), "v1.2.0", Arg.Any<CancellationToken>()).Returns(true);

        await Step().Run(_release, TheProject, TestContext.Current.CancellationToken);

        await _git.DidNotReceiveWithAnyArgs().CreateTag(Arg.Any<IDirectory>(), default!, default, TestContext.Current.CancellationToken);
        await _git.Received().PushTag(Arg.Any<IDirectory>(), "origin", "v1.2.0", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HonoursTheTagPrefix()
    {
        _release = _release with { TagPrefix = "release/" };

        await Step().Run(_release, TheProject, TestContext.Current.CancellationToken);

        await _git.Received().CreateTag(Arg.Any<IDirectory>(), "release/1.2.0", null, Arg.Any<CancellationToken>());
    }

    private GitTag Step() =>
        new(Substitute.For<IWorkflowLog>(), Options.Create(_options), _git, Substitute.For<IFileSystem>());
}
