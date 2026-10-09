using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Octokit;
using Ritten.Changelogs;
using Ritten.Commands;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Engine;
using Ritten.Git;
using Ritten.GitHub;
using Ritten.NuGet;
using Ritten.Reporting;
using Ritten.Tests.Support;
using Ritten.Workflows;

namespace Ritten.Tests.Extensions;

public class WorkflowBuilderExtensionsTests
{
    [Fact]
    public void Registrations_AreIdempotent()
    {
        var services = Builder()
            .AddCommandRunner().AddCommandRunner()
            .AddChangelogs().AddChangelogs()
            .AddDotNet().AddDotNet()
            .AddGit().AddGit()
            .AddNuGet().AddNuGet()
            .AddGitHubClient().AddGitHubClient()
            .AddBuildReporting().AddBuildReporting()
            .Services;

        services.Count(d => d.ServiceType == typeof(ICommandRunner)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IChangelog)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IDotNet)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IGit)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(INuGet)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IGitHubClient)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IGitHubReleaseService)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IWorkflowReport)).ShouldBe(1);
    }

    [Fact]
    public void AddGit_RegistersItsCommandRunnerDependency()
    {
        var services = Builder().AddGit().Services;

        services.Count(d => d.ServiceType == typeof(ICommandRunner)).ShouldBe(1);
    }

    [Fact]
    public void AddDotNet_RegistersItsCommandRunnerDependency()
    {
        var services = Builder().AddDotNet().Services;

        services.Count(d => d.ServiceType == typeof(ICommandRunner)).ShouldBe(1);
    }

    [Fact]
    public void AddBuildReporting_CarriesNoGitHubDependencies()
    {
        // Where the report lands is the active runtime's business; reporting itself must build on
        // any runtime, GitHub nowhere in sight.
        var services = Builder().AddBuildReporting().Services;

        services.ShouldNotContain(d => d.ServiceType == typeof(IGitHubClient));
        services.ShouldNotContain(d => d.ServiceType == typeof(IGitHubCommentService));
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
    public void DotNetBuildSettings_TheFirstProjectIsTheReleasesFace()
    {
        new DotNetBuildSettings { Projects = ["src/A/A.csproj", "src/B/B.csproj"] }.ShippedProjects.ShouldBe(["src/A/A.csproj", "src/B/B.csproj"]);
        new DotNetBuildSettings { Project = "src/A/A.csproj" }.ShippedProjects.ShouldBe(["src/A/A.csproj"]);
        new DotNetBuildSettings().ShippedProjects.ShouldBeEmpty();
    }

    [Fact]
    public void AddDotNetWorkflows_RegistersEverythingTheirJobsUse()
    {
        // Every step of every workflow is checked against the services when the application builds.
        var builder = WorkflowApplication.CreateBuilder();
        builder.AddDotNetWorkflows();

        using var application = builder.Build(_ => null).Value.ShouldNotBeNull();

        application.Workflows.Select(w => w.Name).ShouldBe(["dotnet-tool", "dotnet-package", "dotnet"]);
    }

    private static TestWorkflowBuilder Builder(Dictionary<string, string>? environment = null)
    {
        var builder = new TestWorkflowBuilder();
        builder.Services.AddSingleton(new WorkflowEnvironment((environment ?? []).GetValueOrDefault));
        return builder;
    }
}
