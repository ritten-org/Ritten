using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Runtimes.GitHubActions;
using Ritten.Pipelines.GitHub;
using Ritten.Tests.Support;

namespace Ritten.Tests.Pipelines;

public class CreateGitHubReleaseTests
{
    private readonly IReleaseService _releases = Substitute.For<IReleaseService>();
    private readonly IChangelog _changelogs = Substitute.For<IChangelog>();
    private readonly IPipelineState _state = Substitute.For<IPipelineState>();
    private readonly ChangelogEntry _entry = new() { Version = NuGetVersion.Parse("1.2.0"), Added = ["A thing."] };

    public CreateGitHubReleaseTests()
    {
        SetVersion("1.2.0");
        _state.Get<ChangelogEntry>().Returns(_entry);
        _changelogs.RenderEntry(_entry).Returns("### Added\n\n- A thing.");
    }

    [Fact]
    public async Task SkipsPrereleaseVersions()
    {
        SetVersion("1.2.0-beta.1");

        await Step().Run(TestContext.Current.CancellationToken);

        await _releases.DidNotReceiveWithAnyArgs().Exists(default!, TestContext.Current.CancellationToken);
        await _releases.DidNotReceiveWithAnyArgs().Create(default!, default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SkipsWhenTheReleaseAlreadyExists()
    {
        _releases.Exists("v1.2.0", Arg.Any<CancellationToken>()).Returns(true);

        await Step().Run(TestContext.Current.CancellationToken);

        await _releases.DidNotReceiveWithAnyArgs().Create(default!, default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreatesTheReleaseWithTheRenderedChangelogEntry()
    {
        await Step().Run(TestContext.Current.CancellationToken);

        await _releases.Received().Create("v1.2.0", "v1.2.0", "### Added\n\n- A thing.", Arg.Any<CancellationToken>());
    }

    private void SetVersion(string version) =>
        _state.Get<Project>()
            .Returns(new Project { Name = "My.Package", Version = NuGetVersion.Parse(version) });

    private CreateGitHubRelease Step() =>
        new(Substitute.For<IPipelineLog>(), Options.Create(TestOptions.Git()), _state, _releases, _changelogs);
}
