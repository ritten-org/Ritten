using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ritten.Commands;
using Ritten.NSchema;
using Ritten.NSchema.Steps;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.NSchema;

public class NSchemaStepTests
{
    private readonly INSchema _nschema = Substitute.For<INSchema>();
    private readonly TestReport _report = new();
    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();
    private static readonly NSchemaProject Project = new("schema");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Plan_StopsTheJob_WhenNothingWouldChange()
    {
        _nschema.Plan(Project, null, Arg.Any<CancellationToken>()).Returns(new NSchemaPlanResult(false, ""));

        var result = await new NSchemaPlan(_nschema, _report, _log).Run(Project, null, Ct);

        result.Outcome.IsFailure.ShouldBeFalse();
        result.Outcome.Continue.ShouldBeFalse();
        _report.Sections.ShouldHaveSingleItem().Title.ShouldBe(SectionName.Database.Title);
    }

    [Fact]
    public async Task Plan_ReportsThePlan_AndHandsItOn()
    {
        var plan = new NSchemaPlanResult(true, "| create | main.entries |");
        _nschema.Plan(Project, null, Arg.Any<CancellationToken>()).Returns(plan);

        var result = await new NSchemaPlan(_nschema, _report, _log).Run(Project, null, Ct);

        result.Value.ShouldBe(plan);
        _report.Sections.ShouldHaveSingleItem().Entries.ShouldHaveSingleItem().ShouldBeOfType<ReportParagraph>().Markdown.ShouldContain("main.entries");
    }

    [Fact]
    public async Task FormatCheck_NamesTheUnformattedFiles_AndFails()
    {
        _nschema.VerifyFormatting(Project, Arg.Any<CancellationToken>()).Returns(["tables.sql"]);

        var result = await new NSchemaFormatCheck(_nschema, _report).Run(Project, Ct);

        result.IsFailure.ShouldBeTrue();
        _report.Sections.ShouldHaveSingleItem().Entries.ShouldHaveSingleItem().ShouldBeOfType<ReportParagraph>().Markdown.ShouldContain("`tables.sql`");
    }

    [Fact]
    public async Task Apply_HandsOnTheEnvironmentAndSettings()
    {
        var environment = new NSchemaEnvironment { Name = "pi" };
        var settings = new NSchemaApplySettings { NoLock = true };

        var result = await new NSchemaApply(_nschema, _report).Run(Project, environment, settings, Ct);

        result.IsFailure.ShouldBeFalse();
        await _nschema.Received().Apply(Project, environment, settings, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InADryRun_TheClientPlans_ButAppliesNothing()
    {
        var commands = new FakeCommandRunner();
        commands.Respond(command => command.Arguments.Contains("plan"), new CommandResult(2, "plan", ""));
        var client = new NSchemaClient(new DryRunCommandRunner(_log, commands), Options.Create(new NSchemaOptions { Executable = "nschema" }));

        (await client.Plan(Project, ct: Ct)).Output.ShouldBe("plan");
        await client.Apply(Project, ct: Ct);

        commands.Executed.ShouldHaveSingleItem().Arguments[0].ShouldBe("plan");
        _log.Received().Log(WorkflowLogLevel.Skipped, "Would apply the schema.");
    }

    [Fact]
    public void AnyHost_CanHaveTheClient_WithoutAWorkflow()
    {
        using var services = new ServiceCollection().AddNSchema(options => options.Executable = "/opt/nschema/nschema").BuildServiceProvider();
        using var scope = services.CreateScope();

        scope.ServiceProvider.GetRequiredService<INSchema>().ShouldBeOfType<NSchemaClient>();
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
