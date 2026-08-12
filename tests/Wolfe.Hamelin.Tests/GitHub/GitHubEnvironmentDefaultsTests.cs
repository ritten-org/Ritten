using Wolfe.Hamelin.GitHub;

namespace Wolfe.Hamelin.Tests.GitHub;

public class GitHubEnvironmentDefaultsTests
{
    [Fact]
    public void Apply_ReadsTheActionsEnvironment()
    {
        var options = Apply(new Dictionary<string, string>
        {
            ["GH_TOKEN"] = "token-1",
            ["GITHUB_REPOSITORY_ID"] = "12345",
            ["GITHUB_REF"] = "refs/pull/42/merge",
            ["GITHUB_WORKFLOW"] = "My Workflow"
        });

        options.Token.ShouldBe("token-1");
        options.RepositoryId.ShouldBe(12345);
        options.PullRequestNumber.ShouldBe(42);
        options.IsPullRequest.ShouldBeTrue();
        options.WorkflowName.ShouldBe("My Workflow");
    }

    [Fact]
    public void Apply_FallsBackToTheDefaultGitHubToken()
    {
        var options = Apply(new Dictionary<string, string> { ["GITHUB_TOKEN"] = "token-2" });

        options.Token.ShouldBe("token-2");
    }

    [Fact]
    public void Apply_PrefersGhTokenOverGitHubToken()
    {
        var options = Apply(new Dictionary<string, string>
        {
            ["GH_TOKEN"] = "token-1",
            ["GITHUB_TOKEN"] = "token-2"
        });

        options.Token.ShouldBe("token-1");
    }

    [Fact]
    public void Apply_LeavesPullRequestNumberNullForBranchRefs()
    {
        var options = Apply(new Dictionary<string, string> { ["GITHUB_REF"] = "refs/heads/main" });

        options.PullRequestNumber.ShouldBeNull();
        options.IsPullRequest.ShouldBeFalse();
    }

    [Fact]
    public void Apply_UsesSafeDefaultsOutsideOfActions()
    {
        var options = Apply([]);

        options.Token.ShouldBeNull();
        options.RepositoryId.ShouldBeNull();
        options.PullRequestNumber.ShouldBeNull();
        options.WorkflowName.ShouldBe("Pipeline");
    }

    [Fact]
    public void Apply_IgnoresAMalformedRepositoryId()
    {
        var options = Apply(new Dictionary<string, string> { ["GITHUB_REPOSITORY_ID"] = "not-a-number" });

        options.RepositoryId.ShouldBeNull();
    }

    private static GitHubOptions Apply(Dictionary<string, string> environment)
    {
        var options = new GitHubOptions();
        GitHubEnvironmentDefaults.Apply(options, name => environment.GetValueOrDefault(name));
        return options;
    }
}
