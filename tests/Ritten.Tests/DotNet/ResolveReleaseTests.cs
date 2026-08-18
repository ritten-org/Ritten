using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Git;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.DotNet;

/// <summary>
/// The repository URL is resolved here, once — explicit setting, then the project file, then
/// the origin remote — so no consumer ever coalesces sources again.
/// </summary>
public class ResolveReleaseTests
{
    private readonly IGit _git = Substitute.For<IGit>();
    private readonly DotNetOptions _options = TestOptions.DotNet();

    [Fact]
    public async Task AnExplicitSettingWinsOverEverySource()
    {
        _options.Repository = "https://github.com/configured/repo";

        var release = await Produce(Entry(repository: "https://github.com/csproj/repo"));

        release.Repository.ShouldBe("https://github.com/configured/repo");
        await _git.DidNotReceiveWithAnyArgs().GetRemoteUrl(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TheProjectFileWinsOverTheRemote()
    {
        var release = await Produce(Entry(repository: "https://github.com/csproj/repo"));

        release.Repository.ShouldBe("https://github.com/csproj/repo");
        await _git.DidNotReceiveWithAnyArgs().GetRemoteUrl(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FallsBackToTheOriginRemoteNormalised()
    {
        _git.GetRemoteUrl("origin", Arg.Any<CancellationToken>()).Returns("git@github.com:remote/repo.git");

        var release = await Produce(Entry());

        release.Repository.ShouldBe("https://github.com/remote/repo");
    }

    [Fact]
    public async Task LeavesTheRepositoryUnknownWhenNoSourceHasIt()
    {
        var release = await Produce(Entry());

        release.Repository.ShouldBeNull();
    }

    [Fact]
    public async Task TheFirstProjectIsTheReleasesFace()
    {
        var release = await Produce(Entry(name: "My.Package"), Entry(name: "My.Package.Core"));

        release.Name.ShouldBe("My.Package");
    }

    [Fact]
    public async Task FailsWhenNothingShips()
    {
        var result = await Step().Run(new PackageSet { Packages = [] }, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
    }

    private static Project Entry(string name = "My.Package", string? repository = null) => new()
    {
        Name = name,
        Version = NuGetVersion.Parse("1.2.0"),
        Repository = repository,
        ProjectFile = $"src/{name}/{name}.csproj"
    };

    private async Task<Project> Produce(params Project[] entries)
    {
        var result = await Step().Run(new PackageSet { Packages = entries }, TestContext.Current.CancellationToken);
        result.Outcome.IsFailure.ShouldBeFalse();
        return result.Value.ShouldNotBeNull();
    }

    private ResolveRelease Step() =>
        new(Substitute.For<IWorkflowLog>(), Options.Create(_options), _git);
}
