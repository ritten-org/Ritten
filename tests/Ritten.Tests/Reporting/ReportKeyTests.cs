using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.Tests.Reporting;

public class ReportKeyTests
{
    [Fact]
    public void Of_KeysTheRunByItsWorkflowAndSubject()
    {
        var key = ReportKey.Of(new RunContext { Title = "CI" }, new WorkflowJob("tofu", "check", Subject: "caddy/infrastructure"));

        key.ShouldBe(new ReportKey("ci", "caddy-infrastructure"));
        key.ToString().ShouldBe("ci:caddy-infrastructure");
    }

    [Fact]
    public void Of_KeysARunWithoutASubjectByItsWorkflowAlone()
    {
        ReportKey.Of(new RunContext { Title = "CI" }, new WorkflowJob("tofu", "check")).ToString().ShouldBe("ci");
    }

    [Theory]
    [InlineData("ci")]
    [InlineData("ci:caddy-infrastructure")]
    public void Parse_ReadsWhatToStringWrites(string written)
    {
        ReportKey.Parse(written)?.ToString().ShouldBe(written);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ci:")]
    [InlineData(":caddy")]
    public void Parse_RefusesWhatIsNotAKey(string written)
    {
        ReportKey.Parse(written).ShouldBeNull();
    }
}
