using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Engine.Runs;
using Ritten.GitHub;
using Ritten.Reporting;
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
        runtime.Claims.ShouldContain("GITHUB_BASE_REF");
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
    public void ConfigureServices_DescribesThePullRequestUnderReview()
    {
        var provider = Build(runtimeEnvironment: new Dictionary<string, string>
        {
            ["GITHUB_REF"] = "refs/pull/42/merge",
            ["GITHUB_BASE_REF"] = "main"
        });

        var pullRequest = provider.GetRequiredService<PullRequest>();
        pullRequest.IsPullRequest.ShouldBeTrue();
        pullRequest.Number.ShouldBe(42);
        pullRequest.BaseRef.ShouldBe("main");
    }

    [Fact]
    public void ConfigureServices_SaysSoWhenTheRunReviewsNoPullRequest()
    {
        var provider = Build(runtimeEnvironment: new Dictionary<string, string> { ["GITHUB_REF"] = "refs/heads/main" });

        provider.GetRequiredService<PullRequest>().IsPullRequest.ShouldBeFalse();
    }

    [Fact]
    public void ConfigureServices_ReadsLabelsFromTheGitHubApi()
    {
        var builder = Builder(filteredEnvironment: []);

        new GitHubActionsRuntime().Configure(builder, new Dictionary<string, string>().GetValueOrDefault);

        builder.Services.ShouldContain(d =>
            d.ServiceType == typeof(IPullRequestLabels) && d.ImplementationType == typeof(GitHubPullRequestLabels));
    }

    [Fact]
    public void ConfigureServices_LeavesALabelReadTheHostBrought()
    {
        // Capability defaults are TryAdd: a host that supplied its own implementation keeps it,
        // and the runtime only fills the gap.
        var own = Substitute.For<IPullRequestLabels>();
        var builder = Builder(filteredEnvironment: []);
        builder.Services.AddSingleton(own);

        new GitHubActionsRuntime().Configure(builder, new Dictionary<string, string>().GetValueOrDefault);

        builder.Services.Where(d => d.ServiceType == typeof(IPullRequestLabels))
            .ShouldHaveSingleItem().ImplementationInstance.ShouldBeSameAs(own);
    }

    [Fact]
    public void ConfigureServices_TitlesTheRunAfterTheWorkflow()
    {
        var provider = Build(runtimeEnvironment: new Dictionary<string, string> { ["GITHUB_WORKFLOW"] = "My Workflow" });

        provider.GetRequiredService<RunContext>().Title.ShouldBe("My Workflow");
    }

    [Fact]
    public void ConfigureServices_LeavesTheRunTitleToTheEngineWithoutAWorkflow()
    {
        // Registering nothing lets the engine's default land; claiming the title with a fallback
        // here would shadow one the host declared.
        var provider = Build(runtimeEnvironment: []);

        provider.GetService<RunContext>().ShouldBeNull();
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

        builder.Services.Count(d => d.ServiceType == typeof(IWorkflowResultSink)).ShouldBe(2);
        builder.Services.Count(d => d.ServiceType == typeof(ICommentService)).ShouldBe(1);
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
