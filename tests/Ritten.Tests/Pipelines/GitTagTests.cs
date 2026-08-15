using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.Pipelines.Git;
using Ritten.Tests.Support;

namespace Ritten.Tests.Pipelines;

public class GitTagTests
{
    private static readonly Project TheProject = new() { Name = "My.Package", Version = NuGetVersion.Parse("1.2.0") };

    private readonly IGit _git = Substitute.For<IGit>();
    private readonly GitOptions _options = TestOptions.Git();

    [Fact]
    public async Task SkipsWhenTheTagAlreadyExistsOnOrigin()
    {
        _git.RemoteTagExists("origin", "v1.2.0", Arg.Any<CancellationToken>()).Returns(true);

        await Step().Run(TheProject, TestContext.Current.CancellationToken);

        await _git.DidNotReceiveWithAnyArgs().CreateTag(default!, default, TestContext.Current.CancellationToken);
        await _git.DidNotReceiveWithAnyArgs().PushTag(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreatesAndPushesTheTagWhenItDoesNotExist()
    {
        await Step().Run(TheProject, TestContext.Current.CancellationToken);

        await _git.Received().CreateTag("v1.2.0", null, Arg.Any<CancellationToken>());
        await _git.Received().PushTag("origin", "v1.2.0", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TagsTheConfiguredCommitSha()
    {
        _options.CommitSha = "abc123";

        await Step().Run(TheProject, TestContext.Current.CancellationToken);

        await _git.Received().CreateTag("v1.2.0", "abc123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushesAnExistingLocalTagWithoutRecreatingIt()
    {
        _git.TagExists("v1.2.0", Arg.Any<CancellationToken>()).Returns(true);

        await Step().Run(TheProject, TestContext.Current.CancellationToken);

        await _git.DidNotReceiveWithAnyArgs().CreateTag(default!, default, TestContext.Current.CancellationToken);
        await _git.Received().PushTag("origin", "v1.2.0", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HonoursTheTagPrefix()
    {
        _options.TagPrefix = "release/";

        await Step().Run(TheProject, TestContext.Current.CancellationToken);

        await _git.Received().CreateTag("release/1.2.0", null, Arg.Any<CancellationToken>());
    }

    private GitTag Step() =>
        new(Substitute.For<IPipelineLog>(), Options.Create(_options), _git);
}
