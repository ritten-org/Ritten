using Ritten.GitHub;
using Ritten.Reporting;

namespace Ritten.Tests.GitHub;

public class GitHubCommentServiceTests
{
    [Fact]
    public void MarkerOf_NamesTheWorkflow()
    {
        GitHubGitHubCommentService.MarkerOf(new ReportKey("CI")).ShouldBe("<!-- ritten:ci -->");
    }

    [Fact]
    public void MarkerOf_NamesTheSubjectToo_SoEachSubjectKeepsItsOwnComment()
    {
        GitHubGitHubCommentService.MarkerOf(new ReportKey("CI", "caddy/infrastructure")).ShouldBe("<!-- ritten:ci:caddy-infrastructure -->");
    }
}
