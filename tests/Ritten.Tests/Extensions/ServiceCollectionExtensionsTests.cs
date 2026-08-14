using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Octokit;
using Ritten.Changelogs;
using Ritten.Commands;
using Ritten.DotNet;
using Ritten.Extensions;
using Ritten.Git;
using Ritten.NuGet;
using Ritten.Pipelines;
using Ritten.Pipelines.DotNet;
using Ritten.Pipelines.Git;
using Ritten.Pipelines.NuGet;
using Ritten.Reporting;
using Ritten.Reporting.Sinks;
using Ritten.Runtimes;
using Ritten.Runtimes.GitHubActions;

namespace Ritten.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private static readonly DotNetPackageSettings Settings = new()
    {
        Project = "src/Thing/Thing.csproj",
        Configuration = "Debug",
        Changelog = "HISTORY.md",
        Repository = "https://example.com/thing",
        TagPrefix = "release-",
        Feed = "https://example.com/index.json"
    };

    [Fact]
    public void Registrations_AreIdempotent()
    {
        var services = Services()
            .AddCommandRunner().AddCommandRunner()
            .AddChangelogs(Settings).AddChangelogs(Settings)
            .AddDotNet(Settings).AddDotNet(Settings)
            .AddGit(Settings).AddGit(Settings)
            .AddNuGet(Settings).AddNuGet(Settings)
            .AddGitHubActionsRuntime().AddGitHubActionsRuntime()
            .AddBuildReporting().AddBuildReporting();

        services.Count(d => d.ServiceType == typeof(ICommandRunner)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IChangelog)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IDotNet)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IGit)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(INuGet)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IGitHubClient)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(ICommentService)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IBuildReport)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IReportSink)).ShouldBe(2);
    }

    [Fact]
    public void AddGit_RegistersItsCommandRunnerDependency()
    {
        var services = Services().AddGit(Settings);

        services.Count(d => d.ServiceType == typeof(ICommandRunner)).ShouldBe(1);
    }

    [Fact]
    public void AddDotNet_RegistersItsCommandRunnerDependency()
    {
        var services = Services().AddDotNet(Settings);

        services.Count(d => d.ServiceType == typeof(ICommandRunner)).ShouldBe(1);
    }

    [Fact]
    public void AddBuildReporting_RegistersItsGitHubDependencies()
    {
        var services = Services().AddBuildReporting();

        services.Count(d => d.ServiceType == typeof(IGitHubClient)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(ICommentService)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IReleaseService)).ShouldBe(1);
    }

    [Fact]
    public void AddGitHubActionsRuntime_AppliesTheGivenClientName()
    {
        var provider = Services().AddGitHubActionsRuntime("My.Pipeline").BuildServiceProvider();

        provider.GetRequiredService<IOptions<GitHubOptions>>().Value.ClientName.ShouldBe("My.Pipeline");
    }

    [Fact]
    public void AddGitHubActionsRuntime_KeepsAnExplicitClientNameWhenRedundantlyRegistered()
    {
        var provider = Services().AddGitHubActionsRuntime("My.Pipeline").AddBuildReporting().BuildServiceProvider();

        provider.GetRequiredService<IOptions<GitHubOptions>>().Value.ClientName.ShouldBe("My.Pipeline");
    }

    [Fact]
    public void EachCapability_MapsOnlyItsOwnSliceOfTheSettings()
    {
        var provider = Services()
            .AddDotNet(Settings)
            .AddChangelogs(Settings)
            .AddGit(Settings)
            .AddNuGet(Settings)
            .BuildServiceProvider();

        provider.GetRequiredService<IOptions<DotNetOptions>>().Value.ProjectFile.ShouldBe("src/Thing/Thing.csproj");
        provider.GetRequiredService<IOptions<DotNetOptions>>().Value.Configuration.ShouldBe("Debug");
        provider.GetRequiredService<IOptions<ChangelogOptions>>().Value.File.ShouldBe("HISTORY.md");
        provider.GetRequiredService<IOptions<ChangelogOptions>>().Value.RepositoryUrl.ShouldBe("https://example.com/thing");
        provider.GetRequiredService<IOptions<GitOptions>>().Value.TagPrefix.ShouldBe("release-");
        provider.GetRequiredService<IOptions<NuGetOptions>>().Value.Feed.ShouldBe("https://example.com/index.json");
    }

    private static IServiceCollection Services() => new ServiceCollection();
}
