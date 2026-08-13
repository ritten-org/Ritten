using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Octokit;
using Ritten.Changelogs;
using Ritten.Commands;
using Ritten.DotNet;
using Ritten.Extensions;
using Ritten.Git;
using Ritten.NuGet;
using Ritten.Reporting;
using Ritten.Reporting.Sinks;
using Ritten.Runtimes;
using Ritten.Runtimes.GitHubActions;

namespace Ritten.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Registrations_AreIdempotent()
    {
        var services = Services()
            .AddCommandRunner().AddCommandRunner()
            .AddChangelogs().AddChangelogs()
            .AddDotNet().AddDotNet()
            .AddGit().AddGit()
            .AddNuGet().AddNuGet()
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
        var services = Services().AddGit();

        services.Count(d => d.ServiceType == typeof(ICommandRunner)).ShouldBe(1);
    }

    [Fact]
    public void AddDotNet_RegistersItsCommandRunnerDependency()
    {
        var services = Services().AddDotNet();

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

    private static IServiceCollection Services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        return services;
    }
}
