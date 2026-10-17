using Ritten.Contracts;
using Ritten.GitHub;

namespace Ritten.Tests.GitHub;

public class GitHubCommentServiceTests
{
    private static readonly RunContext Context = new() { Title = "CI" };

    [Fact]
    public void MarkerOf_NamesTheWorkflow()
    {
        GitHubGitHubCommentService.MarkerOf(Context, new WorkflowJob("tofu", "check")).ShouldBe("<!-- ritten:ci -->");
    }

    [Fact]
    public void MarkerOf_NamesTheSubjectToo_SoEachSubjectKeepsItsOwnComment()
    {
        GitHubGitHubCommentService.MarkerOf(Context, new WorkflowJob("tofu", "check", Subject: "caddy/infrastructure"))
            .ShouldBe("<!-- ritten:ci:caddy-infrastructure -->");
    }
}
