using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Octokit;
using Ritten.Changelogs;
using Ritten.Commands;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.GitHub;
using Ritten.NuGet;
using Ritten.Pipelines;
using Ritten.Releases;
using Ritten.Reporting;

namespace Ritten.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private static readonly DotNetToolSettings Settings = new()
    {
        Repository = "https://example.com/thing",
        Build = new DotNetBuildSettings { Project = "src/Thing/Thing.csproj", Configuration = "Debug" },
        Changelog = new ChangelogSettings { File = "HISTORY.md" },
        Release = new ReleaseSettings { TagPrefix = "release-", Feed = "https://example.com/index.json" }
    };

    [Fact]
    public void Registrations_AreIdempotent()
    {
        var services = Services()
            .AddCommandRunner().AddCommandRunner()
            .AddChangelogs(Settings.Changelog).AddChangelogs(Settings.Changelog)
            .AddDotNet(Settings.Build).AddDotNet(Settings.Build)
            .AddGit(Settings.Release.TagPrefix).AddGit(Settings.Release.TagPrefix)
            .AddNuGet(Settings.Release.Feed, ReleaseLine.Major).AddNuGet(Settings.Release.Feed, ReleaseLine.Major)
            .AddGitHubClient().AddGitHubClient()
            .AddBuildReporting().AddBuildReporting();

        services.Count(d => d.ServiceType == typeof(ICommandRunner)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IChangelog)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IDotNet)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IGit)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(INuGet)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IGitHubClient)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IReleaseService)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IBuildReport)).ShouldBe(1);
    }

    [Fact]
    public void AddGit_RegistersItsCommandRunnerDependency()
    {
        var services = Services().AddGit(Settings.Release.TagPrefix);

        services.Count(d => d.ServiceType == typeof(ICommandRunner)).ShouldBe(1);
    }

    [Fact]
    public void AddDotNet_RegistersItsCommandRunnerDependency()
    {
        var services = Services().AddDotNet(Settings.Build);

        services.Count(d => d.ServiceType == typeof(ICommandRunner)).ShouldBe(1);
    }

    [Fact]
    public void AddBuildReporting_CarriesNoGitHubDependencies()
    {
        // Where the report lands is the active runtime's business; reporting itself must build on
        // any runtime, GitHub nowhere in sight.
        var services = Services().AddBuildReporting();

        services.ShouldNotContain(d => d.ServiceType == typeof(IGitHubClient));
        services.ShouldNotContain(d => d.ServiceType == typeof(ICommentService));
    }

    [Fact]
    public void AddGitHubClient_AppliesTheGivenClientName()
    {
        var provider = Services().AddGitHubClient("My.Pipeline").BuildServiceProvider();

        provider.GetRequiredService<IOptions<GitHubClientOptions>>().Value.ClientName.ShouldBe("My.Pipeline");
    }

    [Fact]
    public void AddGitHubClient_KeepsAnExplicitClientNameWhenRedundantlyRegistered()
    {
        var provider = Services().AddGitHubClient("My.Pipeline").AddGitHubClient().BuildServiceProvider();

        provider.GetRequiredService<IOptions<GitHubClientOptions>>().Value.ClientName.ShouldBe("My.Pipeline");
    }

    [Fact]
    public void AddGitHubClient_ReadsTheExplicitTokenFromTheFilteredEnvironment()
    {
        var provider = Services(new Dictionary<string, string> { ["GH_TOKEN"] = "explicit" })
            .AddGitHubClient()
            .BuildServiceProvider();

        provider.GetRequiredService<IOptions<GitHubClientOptions>>().Value.Token.ShouldBe("explicit");
    }

    [Fact]
    public void EachCapability_MapsOnlyItsOwnSliceOfTheSettings()
    {
        var provider = Services()
            .AddDotNet(Settings.Build, Settings.Repository)
            .AddChangelogs(Settings.Changelog)
            .AddGit(Settings.Release.TagPrefix)
            .AddNuGet(Settings.Release.Feed, ReleaseLine.Major)
            .BuildServiceProvider();

        provider.GetRequiredService<IOptions<DotNetOptions>>().Value.ProjectFile.ShouldBe("src/Thing/Thing.csproj");
        provider.GetRequiredService<IOptions<DotNetOptions>>().Value.Configuration.ShouldBe("Debug");
        provider.GetRequiredService<IOptions<DotNetOptions>>().Value.Repository.ShouldBe("https://example.com/thing");
        provider.GetRequiredService<IOptions<ChangelogOptions>>().Value.File.ShouldBe("HISTORY.md");
        provider.GetRequiredService<IOptions<GitOptions>>().Value.TagPrefix.ShouldBe("release-");
        provider.GetRequiredService<IOptions<NuGetOptions>>().Value.Feed.ShouldBe("https://example.com/index.json");
    }

    private static IServiceCollection Services(Dictionary<string, string>? environment = null) =>
        new ServiceCollection().AddSingleton(new PipelineEnvironment((environment ?? []).GetValueOrDefault));
}
