using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Pipelines;
using Ritten.Pipelines.GitHub;
using Ritten.Runtimes.GitHubActions;
using Ritten.Tests.Support;

namespace Ritten.Tests.Pipelines;

public class GitHubReleaseTests
{
    private readonly IReleaseService _releases = Substitute.For<IReleaseService>();
    private readonly IChangelog _changelogs = Substitute.For<IChangelog>();
    private readonly IPipelineState _state = Substitute.For<IPipelineState>();
    private readonly ChangelogEntry _entry = new() { Version = NuGetVersion.Parse("1.2.0"), Added = ["A thing."] };

    public GitHubReleaseTests()
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
        await _releases.DidNotReceiveWithAnyArgs().Create(default!, default!, default!, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SkipsWhenTheReleaseAlreadyExists()
    {
        _releases.Exists("v1.2.0", Arg.Any<CancellationToken>()).Returns(true);

        await Step().Run(TestContext.Current.CancellationToken);

        await _releases.DidNotReceiveWithAnyArgs().Create(default!, default!, default!, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreatesTheReleaseWithTheRenderedChangelogEntry()
    {
        await Step().Run(TestContext.Current.CancellationToken);

        await _releases.Received().Create("v1.2.0", "v1.2.0", "### Added\n\n- A thing.", true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotMarkABackportAsTheLatestRelease()
    {
        // 1.2.0 shipping below 2.0.0 must not displace 2.0.0 as the repository's latest release.
        _state.Get<ReleaseState>()
            .Returns(ReleaseState.Releasable(NuGetVersion.Parse("1.1.0"), NuGetVersion.Parse("2.0.0")));

        await Step().Run(TestContext.Current.CancellationToken);

        await _releases.Received().Create("v1.2.0", "v1.2.0", Arg.Any<string>(), false, Arg.Any<CancellationToken>());
    }

    private void SetVersion(string version) =>
        _state.Get<Project>()
            .Returns(new Project { Name = "My.Package", Version = NuGetVersion.Parse(version) });

    private GitHubRelease Step() =>
        new(Substitute.For<IPipelineLog>(), Options.Create(TestOptions.Git()), _state, _releases, _changelogs);
}
