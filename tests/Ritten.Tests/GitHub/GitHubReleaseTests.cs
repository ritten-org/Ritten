using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.GitHub;
using Ritten.GitHub.Steps;
using Ritten.Releases;
using Ritten.Tests.Support;

namespace Ritten.Tests.GitHub;

public class GitHubReleaseTests
{
    private static readonly ReleaseState Releasable = new ReleaseState(Published: false, LatestInLine: true, null, null);
    private static readonly RepositoryPath Repository = new("example", "repo");

    private readonly IReleaseService _releases = Substitute.For<IReleaseService>();
    private readonly IChangelog _changelogs = Substitute.For<IChangelog>();
    private readonly ChangelogEntry _entry = new() { Version = NuGetVersion.Parse("1.2.0"), Added = ["A thing."] };
    private readonly Changelog _changelog;

    public GitHubReleaseTests()
    {
        _changelog = new Changelog { Entries = [_entry] };
        _changelogs.RenderEntry(_entry).Returns("### Added\n\n- A thing.");
    }

    [Fact]
    public async Task SkipsPrereleaseVersions()
    {
        await Step().Run(Project("1.2.0-beta.1"), _changelog, Releasable, TestContext.Current.CancellationToken);

        await _releases.DidNotReceiveWithAnyArgs().Exists(default!, default!, TestContext.Current.CancellationToken);
        await _releases.DidNotReceiveWithAnyArgs().Create(default!, default!, default!, default!, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SkipsWhenTheReleaseAlreadyExists()
    {
        _releases.Exists(Repository, "v1.2.0", Arg.Any<CancellationToken>()).Returns(true);

        await Step().Run(Project("1.2.0"), _changelog, Releasable, TestContext.Current.CancellationToken);

        await _releases.DidNotReceiveWithAnyArgs().Create(default!, default!, default!, default!, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreatesTheReleaseWithTheRenderedChangelogEntry()
    {
        await Step().Run(Project("1.2.0"), _changelog, Releasable, TestContext.Current.CancellationToken);

        await _releases.Received().Create(Repository, "v1.2.0", "v1.2.0", "### Added\n\n- A thing.", true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotMarkABackportAsTheLatestRelease()
    {
        // 1.2.0 shipping below 2.0.0 must not displace 2.0.0 as the repository's latest release.
        var backport = new ReleaseState(Published: false, LatestInLine: true, NuGetVersion.Parse("1.1.0"), NuGetVersion.Parse("2.0.0"));

        await Step().Run(Project("1.2.0"), _changelog, backport, TestContext.Current.CancellationToken);

        await _releases.Received().Create(Repository, "v1.2.0", "v1.2.0", Arg.Any<string>(), false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FailsWhenTheChangelogHasNoEntryForTheVersion()
    {
        // The gate guarantees a releasable state, so a missing entry here is genuine drift.
        var result = await Step().Run(Project("1.3.0"), _changelog, Releasable, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("1.3.0");
    }

    [Fact]
    public async Task FailsWhenTheRepositoryCannotBeDetermined()
    {
        var result = await Step().Run(Project("1.2.0") with { Repository = null }, _changelog, Releasable, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("repository");
    }

    private static Project Project(string version) =>
        new() { Name = "My.Package", Version = NuGetVersion.Parse(version), Repository = "https://github.com/example/repo" };

    private GitHubRelease Step() =>
        new(Substitute.For<IPipelineLog>(), Options.Create(TestOptions.Git()), _releases, _changelogs);
}
