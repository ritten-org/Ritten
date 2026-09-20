using Ritten.Forgejo;

namespace Ritten.Tests.Forgejo;

public class ForgejoActionsOptionsTests
{
    private static ForgejoActionsOptions Configured(Dictionary<string, string> environment)
    {
        var options = new ForgejoActionsOptions();
        ForgejoActionsOptions.ConfigureFromEnvironment(options, name => environment.GetValueOrDefault(name));
        return options;
    }

    [Fact]
    public void RunUrl_IsTheRunsPageByNumberNotId()
    {
        var options = Configured(new Dictionary<string, string>
        {
            ["FORGEJO_SERVER_URL"] = "https://code.example.com/",
            ["FORGEJO_REPOSITORY"] = "tom/lab",
            ["FORGEJO_RUN_NUMBER"] = "42",
            ["FORGEJO_RUN_ID"] = "9001"
        });

        options.RunUrl.ShouldBe("https://code.example.com/tom/lab/actions/runs/42");
        options.RunId.ShouldBe(9001);
    }

    [Fact]
    public void ReadsTheGitHubMirrorWhenTheNativeNameIsAbsent()
    {
        var options = Configured(new Dictionary<string, string>
        {
            ["GITHUB_SERVER_URL"] = "https://code.example.com",
            ["GITHUB_REPOSITORY"] = "tom/lab",
            ["GITHUB_RUN_NUMBER"] = "7",
            ["GITHUB_WORKFLOW"] = "deploy"
        });

        options.RunUrl.ShouldBe("https://code.example.com/tom/lab/actions/runs/7");
        options.Workflow.ShouldBe("deploy");
    }

    [Fact]
    public void ReadsThePullRequestFromTheRefItCheckedOut()
    {
        // The only place the runner states the number: there is no FORGEJO_PULL_REQUEST_NUMBER.
        var options = Configured(new Dictionary<string, string>
        {
            ["FORGEJO_REF"] = "refs/pull/108/head",
            ["FORGEJO_BASE_REF"] = "main"
        });

        options.PullRequestNumber.ShouldBe(108);
        options.BaseRef.ShouldBe("main");
        options.IsPullRequest.ShouldBeTrue();
    }

    [Fact]
    public void IsNotAPullRequestOnABranchPush()
    {
        var options = Configured(new Dictionary<string, string> { ["FORGEJO_REF"] = "refs/heads/main" });

        options.PullRequestNumber.ShouldBeNull();
        options.IsPullRequest.ShouldBeFalse();
    }

    [Fact]
    public void Token_ComesFromTheMirrorBecauseThatIsWhatAWorkflowPassesThrough()
    {
        // `secrets.GITHUB_TOKEN` is the spelling every Forgejo workflow already uses.
        Configured(new Dictionary<string, string> { ["GITHUB_TOKEN"] = "abc123" }).Token.ShouldBe("abc123");
    }

    [Fact]
    public void ApiUrl_HangsOffTheInstanceTheRunnerNamed()
    {
        Configured(new Dictionary<string, string> { ["FORGEJO_SERVER_URL"] = "https://code.example.com/" })
            .ApiUrl.ShouldBe("https://code.example.com/api/v1/");
    }

    [Fact]
    public void ApiUrl_IsNullWhenTheInstanceIsUnknown()
    {
        Configured([]).ApiUrl.ShouldBeNull();
    }

    [Fact]
    public void RunUrl_IsNullOutsideARun()
    {
        Configured([]).RunUrl.ShouldBeNull();
    }
}
