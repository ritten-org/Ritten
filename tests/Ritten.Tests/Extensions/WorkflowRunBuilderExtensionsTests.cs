using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Octokit;
using Ritten.Changelogs;
using Ritten.Commands;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.GitHub;
using Ritten.NuGet;
using Ritten.Workflows;
using Ritten.Releases;
using Ritten.Reporting;
using Ritten.Tests.Core.Helpers;

namespace Ritten.Tests.Extensions;

public class WorkflowRunBuilderExtensionsTests
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
        var services = Builder()
            .AddCommandRunner().AddCommandRunner()
            .AddChangelogs(Settings.Changelog).AddChangelogs(Settings.Changelog)
            .AddDotNet(Settings.Build).AddDotNet(Settings.Build)
            .AddGit(Settings.Release.TagPrefix).AddGit(Settings.Release.TagPrefix)
            .AddNuGet(Settings.Release.Feed, ReleaseLine.Major).AddNuGet(Settings.Release.Feed, ReleaseLine.Major)
            .AddGitHubClient().AddGitHubClient()
            .AddBuildReporting().AddBuildReporting()
            .Services;

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
        var services = Builder().AddGit(Settings.Release.TagPrefix).Services;

        services.Count(d => d.ServiceType == typeof(ICommandRunner)).ShouldBe(1);
    }

    [Fact]
    public void AddDotNet_RegistersItsCommandRunnerDependency()
    {
        var services = Builder().AddDotNet(Settings.Build).Services;

        services.Count(d => d.ServiceType == typeof(ICommandRunner)).ShouldBe(1);
    }

    [Fact]
    public void AddBuildReporting_CarriesNoGitHubDependencies()
    {
        // Where the report lands is the active runtime's business; reporting itself must build on
        // any runtime, GitHub nowhere in sight.
        var services = Builder().AddBuildReporting().Services;

        services.ShouldNotContain(d => d.ServiceType == typeof(IGitHubClient));
        services.ShouldNotContain(d => d.ServiceType == typeof(ICommentService));
    }

    [Fact]
    public void AddGitHubClient_AppliesTheGivenClientName()
    {
        var provider = Builder().AddGitHubClient("My.Workflow").Services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<GitHubClientOptions>>().Value.ClientName.ShouldBe("My.Workflow");
    }

    [Fact]
    public void AddGitHubClient_KeepsAnExplicitClientNameWhenRedundantlyRegistered()
    {
        var provider = Builder().AddGitHubClient("My.Workflow").AddGitHubClient().Services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<GitHubClientOptions>>().Value.ClientName.ShouldBe("My.Workflow");
    }

    [Fact]
    public void AddGitHubClient_ReadsTheExplicitTokenFromTheFilteredEnvironment()
    {
        var provider = Builder(new Dictionary<string, string> { ["GH_TOKEN"] = "explicit" })
            .AddGitHubClient()
            .Services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<GitHubClientOptions>>().Value.Token.ShouldBe("explicit");
    }

    [Fact]
    public void EachCapability_MapsOnlyItsOwnSliceOfTheSettings()
    {
        var provider = Builder()
            .AddDotNet(Settings.Build, Settings.Repository)
            .AddChangelogs(Settings.Changelog)
            .AddGit(Settings.Release.TagPrefix)
            .AddNuGet(Settings.Release.Feed, ReleaseLine.Major)
            .Services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<DotNetOptions>>().Value.ProjectFile.ShouldBe("src/Thing/Thing.csproj");
        provider.GetRequiredService<IOptions<DotNetOptions>>().Value.Configuration.ShouldBe("Debug");
        provider.GetRequiredService<IOptions<DotNetOptions>>().Value.Repository.ShouldBe("https://example.com/thing");
        provider.GetRequiredService<IOptions<ChangelogOptions>>().Value.File.ShouldBe("HISTORY.md");
        provider.GetRequiredService<IOptions<GitOptions>>().Value.TagPrefix.ShouldBe("release-");
        provider.GetRequiredService<IOptions<NuGetOptions>>().Value.Feed.ShouldBe("https://example.com/index.json");
    }

    private static WorkflowRunBuilder Builder(Dictionary<string, string>? environment = null)
    {
        var builder = WorkflowRunBuilderHelpers.Create();
        builder.Services.AddSingleton(new WorkflowEnvironment((environment ?? []).GetValueOrDefault));
        return builder;
    }
}
