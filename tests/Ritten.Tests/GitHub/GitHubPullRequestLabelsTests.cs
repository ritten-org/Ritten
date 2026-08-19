using Microsoft.Extensions.Options;
using Octokit;
using Ritten.GitHub;

namespace Ritten.Tests.GitHub;

public class GitHubPullRequestLabelsTests
{
    private readonly IGitHubClient _client = Substitute.For<IGitHubClient>();

    [Fact]
    public async Task Read_ReturnsTheLabelsWithTheirMetadata()
    {
        _client.Issue.Labels.GetAllForIssue(12345, 42)
            .Returns([GitHubLabel("breaking-approved", "d73a4a", "Approves breaking changes."), GitHubLabel("documentation", "0075ca", "")]);
        var labels = Service(new GitHubActionsOptions { RepositoryId = 12345, PullRequestNumber = 42 });

        var read = (await labels.Get(TestContext.Current.CancellationToken)).ShouldNotBeNull();

        read.Select(l => l.Name).ShouldBe(["breaking-approved", "documentation"]);
        read[0].Color.ShouldBe("d73a4a");
        read[0].Description.ShouldBe("Approves breaking changes.");
    }

    [Fact]
    public async Task Read_IsEmptyWhenThePullRequestCarriesNoLabels()
    {
        _client.Issue.Labels.GetAllForIssue(12345, 42).Returns([]);
        var labels = Service(new GitHubActionsOptions { RepositoryId = 12345, PullRequestNumber = 42 });

        (await labels.Get(TestContext.Current.CancellationToken)).ShouldNotBeNull().ShouldBeEmpty();
    }

    [Fact]
    public async Task Read_AnswersNothingOutsideAPullRequest()
    {
        // Null, not empty: a step must be able to tell "not labelled" from "nothing to ask".
        var labels = Service(new GitHubActionsOptions { RepositoryId = 12345 });

        (await labels.Get(TestContext.Current.CancellationToken)).ShouldBeNull();
        await _client.Issue.Labels.DidNotReceive().GetAllForIssue(Arg.Any<long>(), Arg.Any<long>());
    }

    [Fact]
    public async Task Read_AnswersNothingWithoutARepository()
    {
        var labels = Service(new GitHubActionsOptions { PullRequestNumber = 42 });

        (await labels.Get(TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    private GitHubPullRequestLabels Service(GitHubActionsOptions options) => new(_client, Options.Create(options));

    private static Label GitHubLabel(string name, string color, string description) => new(0, "", name, "", color, description, false);
}
