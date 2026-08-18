using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.GitHub;
using Ritten.Reporting.Sinks;
using Ritten.Tests.Engine.Helpers;

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
    public void IsDebug_HonoursTheRunnersDebugFlag()
    {
        // RUNNER_DEBUG is claimed, so only this runtime may honour it: exported in a local shell
        // it belongs to no runtime that's present, and stays an ordinary variable.
        var runtime = new GitHubActionsRuntime();

        runtime.IsDebug(new Dictionary<string, string> { ["RUNNER_DEBUG"] = "1" }.GetValueOrDefault).ShouldBeTrue();
        runtime.IsDebug(new Dictionary<string, string>().GetValueOrDefault).ShouldBeFalse();
        runtime.Claims.ShouldContain("RUNNER_DEBUG");
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

        provider.GetRequiredService<IOptions<RunContext>>().Value.Title.ShouldBe("Workflow");
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
        var builder = Builder(filteredEnvironment: []);

        new GitHubActionsRuntime().Configure(builder, new Dictionary<string, string>().GetValueOrDefault);

        builder.Services.Count(d => d.ServiceType == typeof(IReportSink)).ShouldBe(2);
        builder.Services.Count(d => d.ServiceType == typeof(ICommentService)).ShouldBe(1);
        builder.Services.ShouldContain(d => d.ImplementationType == typeof(PendingCommentReporter));
    }

    private static ServiceProvider Build(
        Dictionary<string, string> runtimeEnvironment,
        Dictionary<string, string>? filteredEnvironment = null)
    {
        var builder = Builder(filteredEnvironment ?? []);
        new GitHubActionsRuntime().Configure(builder, runtimeEnvironment.GetValueOrDefault);
        return builder.Services.BuildServiceProvider();
    }

    private static WorkflowRunBuilder Builder(Dictionary<string, string> filteredEnvironment)
    {
        var builder = WorkflowRunBuilderHelpers.Create();
        builder.Services.AddSingleton(new WorkflowEnvironment(filteredEnvironment.GetValueOrDefault));
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        return builder;
    }
}
