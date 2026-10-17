using Ritten.Contracts;
using Ritten.Forgejo;

namespace Ritten.Tests.Forgejo;

public class ForgejoCommentServiceTests
{
    private static readonly RunContext Context = new() { Title = "CI" };

    [Fact]
    public void MarkerOf_NamesTheWorkflow()
    {
        ForgejoCommentService.MarkerOf(Context, new WorkflowJob("tofu", "check")).ShouldBe("<!-- ritten:ci -->");
    }

    [Fact]
    public void MarkerOf_NamesTheSubjectToo_SoEachSubjectKeepsItsOwnComment()
    {
        ForgejoCommentService.MarkerOf(Context, new WorkflowJob("tofu", "check", Subject: "caddy/infrastructure"))
            .ShouldBe("<!-- ritten:ci:caddy-infrastructure -->");
    }
}
