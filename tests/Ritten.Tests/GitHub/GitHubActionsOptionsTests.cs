using Ritten.GitHub;

namespace Ritten.Tests.GitHub;

public class GitHubActionsOptionsTests
{
    [Fact]
    public void ConfigureFromEnvironment_ReadsTheActionsEnvironment()
    {
        var options = Configure(new Dictionary<string, string>
        {
            ["GITHUB_REPOSITORY_ID"] = "12345",
            ["GITHUB_REF"] = "refs/pull/42/merge",
            ["GITHUB_BASE_REF"] = "main",
            ["GITHUB_STEP_SUMMARY"] = "/tmp/summary.md",
            ["GITHUB_SERVER_URL"] = "https://github.com",
            ["GITHUB_REPOSITORY"] = "example/repo",
            ["GITHUB_RUN_ID"] = "987654"
        });

        options.RepositoryId.ShouldBe(12345);
        options.PullRequestNumber.ShouldBe(42);
        options.IsPullRequest.ShouldBeTrue();
        options.BaseRef.ShouldBe("main");
        options.SummaryFile.ShouldBe("/tmp/summary.md");
        options.RunUrl.ShouldBe("https://github.com/example/repo/actions/runs/987654");
    }

    [Fact]
    public void ConfigureFromEnvironment_LeavesTheRunUrlNullWhenAnyPartIsMissing()
    {
        var options = Configure(new Dictionary<string, string>
        {
            ["GITHUB_SERVER_URL"] = "https://github.com",
            ["GITHUB_REPOSITORY"] = "example/repo"
        });

        options.RunUrl.ShouldBeNull();
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

        options.RepositoryId.ShouldBeNull();
        options.PullRequestNumber.ShouldBeNull();
        options.SummaryFile.ShouldBeNull();
        options.RunUrl.ShouldBeNull();
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
        var options = new GitHubActionsOptions
        {
            RepositoryId = 99,
            PullRequestNumber = 7,
            BaseRef = "stale",
            SummaryFile = "/stale/summary.md",
            RunUrl = "https://github.com/example/repo/actions/runs/1"
        };

        GitHubActionsOptions.ConfigureFromEnvironment(options, _ => null);

        options.RepositoryId.ShouldBeNull();
        options.PullRequestNumber.ShouldBeNull();
        options.BaseRef.ShouldBeNull();
        options.SummaryFile.ShouldBeNull();
        options.RunUrl.ShouldBeNull();
    }

    [Theory]
    // The runner sets RUNNER_DEBUG=1; ACTIONS_STEP_DEBUG is the secret asking for it, and never
    // reaches the step, so reading that instead would silently never fire.
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("true", false)]
    [InlineData(null, false)]
    public void IsDebug_ReadsWhatTheRunnerActuallySets(string? value, bool expected)
    {
        var environment = new Dictionary<string, string>();
        if (value is not null)
        {
            environment[GitHubEnvironment.RunnerDebug] = value;
        }

        GitHubEnvironment.IsDebug(environment.GetValueOrDefault).ShouldBe(expected);
    }

    private static GitHubActionsOptions Configure(Dictionary<string, string> environment)
    {
        var options = new GitHubActionsOptions();
        GitHubActionsOptions.ConfigureFromEnvironment(options, environment.GetValueOrDefault);
        return options;
    }
}
