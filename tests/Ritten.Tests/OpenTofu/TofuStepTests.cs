using Ritten.OpenTofu;
using Ritten.OpenTofu.Steps;
using Ritten.Reporting;

namespace Ritten.Tests.OpenTofu;

public class TofuStepTests
{
    private readonly IOpenTofu _tofu = Substitute.For<IOpenTofu>();
    private readonly IWorkflowReport _report = new TestReport();
    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();

    [Fact]
    public async Task Plan_StopsTheJobWhenThereIsNothingToApply()
    {
        // NothingToDo rather than Successful: the steps behind this one exist to apply a change,
        // and there isn't one. The job still succeeds.
        _tofu.Plan(null, Arg.Any<CancellationToken>()).Returns(new TofuPlanResult(false, "No changes."));

        var result = await new TofuPlan(_tofu, _report, _log).Run(null, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeFalse();
        result.Outcome.Continue.ShouldBeFalse();
    }

    [Fact]
    public async Task Plan_ProducesTheResultForTheStepsAfterIt()
    {
        // The package reports the plan; what the plan means — a change to review, drift to
        // page about — is the workflow's call, so the result is handed on rather than consumed.
        var plan = new TofuPlanResult(true, "  + resource \"new\"");
        _tofu.Plan(null, Arg.Any<CancellationToken>()).Returns(plan);

        var result = await new TofuPlan(_tofu, _report, _log).Run(null, TestContext.Current.CancellationToken);

        result.Value.ShouldBe(plan);
    }

    [Fact]
    public async Task TheStepsHandTheResolvedEnvironmentToTheClient()
    {
        var environment = new TofuEnvironment();
        _tofu.Plan(environment, Arg.Any<CancellationToken>()).Returns(new TofuPlanResult(true, "~"));

        await new TofuInit(_tofu).Run(environment, TestContext.Current.CancellationToken);
        await new TofuPlan(_tofu, _report, _log).Run(environment, TestContext.Current.CancellationToken);
        await new TofuApply(_tofu, _report).Run(environment, TestContext.Current.CancellationToken);

        await _tofu.Received().Init(environment, Arg.Any<CancellationToken>());
        await _tofu.Received().Plan(environment, Arg.Any<CancellationToken>());
        await _tofu.Received().Apply(environment, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Plan_CarriesOnAndReportsTheDiffWhenSomethingMoves()
    {
        _tofu.Plan(null, Arg.Any<CancellationToken>()).Returns(new TofuPlanResult(true, "  ~ resource \"dns\"\n  + resource \"new\""));

        var result = await new TofuPlan(_tofu, _report, _log).Run(null, TestContext.Current.CancellationToken);

        result.Outcome.Continue.ShouldBeTrue();
        var note = _report.Sections.ShouldHaveSingleItem().Entries.ShouldHaveSingleItem().ShouldBeOfType<ReportParagraph>();
        // OpenTofu's own markers promoted to the first column, where a diff renderer colours them.
        note.Markdown.ShouldContain("!  ~ resource \"dns\"");
        note.Markdown.ShouldContain("+  + resource \"new\"");
    }

    [Fact]
    public async Task FormatCheck_NamesTheFilesSoTheReportIsActionable()
    {
        _tofu.VerifyFormatting(Arg.Any<CancellationToken>()).Returns<IReadOnlyList<string>?>(["main.tf", "dns.tf"]);

        var result = await new TofuFormatCheck(_tofu, _report).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        var failure = _report.Sections.ShouldHaveSingleItem().Entries.ShouldHaveSingleItem().ShouldBeOfType<ReportParagraph>();
        failure.Markdown.ShouldContain("`main.tf`");
        failure.Markdown.ShouldContain("`dns.tf`");
    }

    [Fact]
    public async Task FormatCheck_PassesWhenNothingNeedsFormatting()
    {
        _tofu.VerifyFormatting(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<string>?)null);

        var result = await new TofuFormatCheck(_tofu, _report).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
    }

    private sealed class TestReport : IWorkflowReport
    {
        private readonly List<ReportSection> _sections = [];

        public IReadOnlyList<ReportSection> Sections => _sections;

        public ReportSection Section(string title)
        {
            var existing = _sections.Find(s => s.Title == title);
            if (existing is not null)
            {
                return existing;
            }

            var section = new ReportSection(title);
            _sections.Add(section);
            return section;
        }
    }
}
