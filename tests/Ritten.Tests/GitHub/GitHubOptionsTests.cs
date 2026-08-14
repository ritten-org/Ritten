using Ritten.Runtimes.GitHubActions;

namespace Ritten.Tests.GitHub;

public class GitHubOptionsTests
{
    [Fact]
    public void ConfigureFromEnvironment_ReadsTheActionsEnvironment()
    {
        var options = Configure(new Dictionary<string, string>
        {
            ["GH_TOKEN"] = "token-1",
            ["GITHUB_REPOSITORY_ID"] = "12345",
            ["GITHUB_REF"] = "refs/pull/42/merge",
            ["GITHUB_WORKFLOW"] = "My Workflow",
            ["GITHUB_ACTIONS"] = "true",
            ["GITHUB_STEP_SUMMARY"] = "/tmp/summary.md"
        });

        options.Token.ShouldBe("token-1");
        options.RepositoryId.ShouldBe(12345);
        options.PullRequestNumber.ShouldBe(42);
        options.IsPullRequest.ShouldBeTrue();
        options.WorkflowName.ShouldBe("My Workflow");
        options.IsEnabled.ShouldBeTrue();
        options.SummaryFile.ShouldBe("/tmp/summary.md");
    }

    [Fact]
    public void ConfigureFromEnvironment_FallsBackToTheDefaultGitHubToken()
    {
        var options = Configure(new Dictionary<string, string> { ["GITHUB_TOKEN"] = "token-2" });

        options.Token.ShouldBe("token-2");
    }

    [Fact]
    public void ConfigureFromEnvironment_PrefersGhTokenOverGitHubToken()
    {
        var options = Configure(new Dictionary<string, string>
        {
            ["GH_TOKEN"] = "token-1",
            ["GITHUB_TOKEN"] = "token-2"
        });

        options.Token.ShouldBe("token-1");
    }

    [Fact]
    public void ConfigureFromEnvironment_LeavesPullRequestNumberNullForBranchRefs()
    {
        var options = Configure(new Dictionary<string, string> { ["GITHUB_REF"] = "refs/heads/main" });

        options.PullRequestNumber.ShouldBeNull();
        options.IsPullRequest.ShouldBeFalse();
    }

    [Fact]
    public void ConfigureFromEnvironment_UsesSafeDefaultsOutsideOfActions()
    {
        var options = Configure([]);

        options.Token.ShouldBeNull();
        options.RepositoryId.ShouldBeNull();
        options.PullRequestNumber.ShouldBeNull();
        options.WorkflowName.ShouldBe("Pipeline");
        options.IsEnabled.ShouldBeFalse();
        options.SummaryFile.ShouldBeNull();
    }

    [Fact]
    public void ConfigureFromEnvironment_IgnoresAMalformedRepositoryId()
    {
        var options = Configure(new Dictionary<string, string> { ["GITHUB_REPOSITORY_ID"] = "not-a-number" });

        options.RepositoryId.ShouldBeNull();
    }

    [Fact]
    public void ConfigureFromEnvironment_ClearsValuesTheEnvironmentDoesNotSet()
    {
        // The environment is applied wholesale and configuration is bound over the top, so this
        // must not merge with whatever the options already held.
        var options = new GitHubOptions
        {
            Token = "stale-token",
            RepositoryId = 99,
            PullRequestNumber = 7,
            IsEnabled = true,
            SummaryFile = "/stale/summary.md"
        };

        GitHubOptions.ConfigureFromEnvironment(options, _ => null);

        options.Token.ShouldBeNull();
        options.RepositoryId.ShouldBeNull();
        options.PullRequestNumber.ShouldBeNull();
        options.IsEnabled.ShouldBeFalse();
        options.SummaryFile.ShouldBeNull();
    }

    [Fact]
    public void ConfigureFromEnvironment_KeepsTheDefaultWorkflowNameWhenUnset()
    {
        var options = new GitHubOptions { WorkflowName = "Configured Workflow" };

        GitHubOptions.ConfigureFromEnvironment(options, _ => null);

        options.WorkflowName.ShouldBe("Configured Workflow");
    }

    private static GitHubOptions Configure(Dictionary<string, string> environment)
    {
        var options = new GitHubOptions();
        GitHubOptions.ConfigureFromEnvironment(options, environment.GetValueOrDefault);
        return options;
    }
}
