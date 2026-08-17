using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.GitHub;
using Ritten.Reporting.Sinks;

namespace Ritten.Tests.GitHub;

public class GitHubActionsRuntimeTests
{
    [Fact]
    public void DetectsOnTheActionsMarkerAndClaimsIt()
    {
        var runtime = new GitHubActionsRuntime();

        runtime.Markers.ShouldBe(["GITHUB_ACTIONS"]);
        runtime.Claims.ShouldContain("GITHUB_ACTIONS");
        runtime.Claims.ShouldContain("GITHUB_TOKEN");
        runtime.Claims.ShouldNotContain("GH_TOKEN", "an explicit GH_TOKEN is the user's instruction to the GitHub client, not the runner's");
    }

    [Fact]
    public void ConfigureServices_ReadsTheActionsContextFromItsClaims()
    {
        var provider = Build(runtimeEnvironment: new Dictionary<string, string>
        {
            ["GITHUB_REPOSITORY_ID"] = "12345",
            ["GITHUB_REF"] = "refs/pull/42/merge",
            ["GITHUB_STEP_SUMMARY"] = "/tmp/summary.md"
        });

        var options = provider.GetRequiredService<IOptions<GitHubActionsOptions>>().Value;
        options.RepositoryId.ShouldBe(12345);
        options.PullRequestNumber.ShouldBe(42);
        options.SummaryFile.ShouldBe("/tmp/summary.md");
    }

    [Fact]
    public void ConfigureServices_TitlesTheRunAfterTheWorkflow()
    {
        var provider = Build(runtimeEnvironment: new Dictionary<string, string> { ["GITHUB_WORKFLOW"] = "My Workflow" });

        provider.GetRequiredService<IOptions<RunContext>>().Value.Title.ShouldBe("My Workflow");
    }

    [Fact]
    public void ConfigureServices_LeavesTheRunTitleAloneWithoutAWorkflow()
    {
        var provider = Build(runtimeEnvironment: []);

        provider.GetRequiredService<IOptions<RunContext>>().Value.Title.ShouldBe("Pipeline");
    }

    [Fact]
    public void ConfigureServices_OffersTheWorkflowTokenToTheGitHubClient()
    {
        // On this runtime the forge that triggered the run is GitHub itself, so its token is a
        // valid ambient credential for the destination client.
        var provider = Build(runtimeEnvironment: new Dictionary<string, string> { ["GITHUB_TOKEN"] = "ambient" });

        provider.GetRequiredService<IOptions<GitHubClientOptions>>().Value.Token.ShouldBe("ambient");
    }

    [Fact]
    public void ConfigureServices_PrefersAnExplicitTokenOverTheWorkflows()
    {
        var provider = Build(
            runtimeEnvironment: new Dictionary<string, string> { ["GITHUB_TOKEN"] = "ambient" },
            filteredEnvironment: new Dictionary<string, string> { ["GH_TOKEN"] = "explicit" });

        provider.GetRequiredService<IOptions<GitHubClientOptions>>().Value.Token.ShouldBe("explicit");
    }

    [Fact]
    public void ConfigureServices_RegistersTheReportChannels()
    {
        var services = Services(filteredEnvironment: []);

        new GitHubActionsRuntime().ConfigureServices(services, new Dictionary<string, string>().GetValueOrDefault);

        services.Count(d => d.ServiceType == typeof(IReportSink)).ShouldBe(2);
        services.Count(d => d.ServiceType == typeof(ICommentService)).ShouldBe(1);
        services.ShouldContain(d => d.ImplementationType == typeof(PendingCommentReporter));
    }

    private static ServiceProvider Build(
        Dictionary<string, string> runtimeEnvironment,
        Dictionary<string, string>? filteredEnvironment = null)
    {
        var services = Services(filteredEnvironment ?? []);
        new GitHubActionsRuntime().ConfigureServices(services, runtimeEnvironment.GetValueOrDefault);
        return services.BuildServiceProvider();
    }

    private static ServiceCollection Services(Dictionary<string, string> filteredEnvironment)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new PipelineEnvironment(filteredEnvironment.GetValueOrDefault));
        services.AddSingleton(Substitute.For<IPipelineLog>());
        return services;
    }
}
