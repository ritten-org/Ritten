using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Forgejo;
using Ritten.Reporting.Sinks;

namespace Ritten.Tests.Forgejo;

public class ForgejoActionsRuntimeTests
{
    private static readonly Dictionary<string, string> Environment = new()
    {
        ["FORGEJO_ACTIONS"] = "true",
        ["GITHUB_ACTIONS"] = "true",
        ["FORGEJO_SERVER_URL"] = "https://code.example.com",
        ["FORGEJO_REPOSITORY"] = "tom/lab",
        ["FORGEJO_RUN_NUMBER"] = "42",
        ["FORGEJO_WORKFLOW"] = "obsidian main"
    };

    [Fact]
    public void MatchesOnTheNativeMarkerAndClaimsTheMirror()
    {
        var runtime = new ForgejoActionsRuntime();

        runtime.Markers.ShouldBe(["FORGEJO_ACTIONS"]);
        runtime.Claims.ShouldContain("GITHUB_ACTIONS");
        runtime.Claims.ShouldContain("GITHUB_RUN_NUMBER");
    }

    [Fact]
    public void Configure_OffersTheRunsFactsAndItsTitle()
    {
        var builder = WorkflowApplication.CreateBuilder();

        new ForgejoActionsRuntime().Configure(builder, name => Environment.GetValueOrDefault(name));
        using var services = builder.Services.BuildServiceProvider();

        services.GetRequiredService<IOptions<ForgejoActionsOptions>>().Value.RunUrl.ShouldBe("https://code.example.com/tom/lab/actions/runs/42");
        services.GetRequiredService<RunContext>().Title.ShouldBe("obsidian main");
        // Declared rather than resolved: the sinks share a constructor dependency on the run's
        // log, which only a run assembles.
        builder.Services.ShouldContain(d => d.ServiceType == typeof(IWorkflowResultSink) && d.ImplementationType == typeof(ForgejoJobSummaryResultSink));
    }

    [Fact]
    public void IsDebug_FollowsTheRunnersFlag()
    {
        new ForgejoActionsRuntime().IsDebug(name => name == "RUNNER_DEBUG" ? "1" : null).ShouldBeTrue();
        new ForgejoActionsRuntime().IsDebug(_ => null).ShouldBeFalse();
    }
}
