using Hamelin;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Wolfe.Hamelin.Changelogs;
using Wolfe.Hamelin.DotNet;
using Wolfe.Hamelin.GitHub;
using Wolfe.Hamelin.Pipelines.GitHub;
using Wolfe.Hamelin.Tests.Support;

namespace Wolfe.Hamelin.Tests.Pipelines;

public class CreateGitHubReleaseTests
{
    private readonly IReleaseService _releases = Substitute.For<IReleaseService>();
    private readonly IChangelog _changelogs = Substitute.For<IChangelog>();
    private readonly IPipelineContext _context = Substitute.For<IPipelineContext>();
    private readonly ChangelogEntry _entry = new() { Version = NuGetVersion.Parse("1.2.0"), Added = ["A thing."] };

    public CreateGitHubReleaseTests()
    {
        SetVersion("1.2.0");
        _context.State.Get<ChangelogEntry>(Arg.Any<string>()).Returns(_entry);
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
        _context.State.Get<Project>(Arg.Any<string>())
            .Returns(new Project { Name = "My.Package", Version = NuGetVersion.Parse(version) });

    private CreateGitHubRelease Step() =>
        new(NullLogger<CreateGitHubRelease>.Instance, Options.Create(TestOptions.Git()), _context, _releases, _changelogs);
}
