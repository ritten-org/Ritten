using Ritten.Commands;
using Ritten.Npm;
using Ritten.Npm.Steps;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Npm;

public class NpmTests
{
    private readonly FakeCommandRunner _commands = new();
    private readonly TestReport _report = new();
    private static readonly NpmProject Project = new("ui");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Ritten.Npm.Npm Npm() => new(_commands);

    [Fact]
    public async Task Install_IsNpmCi_InTheProject()
    {
        await Npm().Install(Project, Ct);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Path.ShouldBe("npm");
        command.Arguments.ShouldBe(["ci", "--no-audit", "--no-fund"]);
        command.WorkingDirectory.ShouldBe("ui");
        command.ThrowsOnError.ShouldBeTrue();
    }

    [Fact]
    public async Task Run_RunsTheScript_AndAnswersWithItsOutput_RatherThanThrowing()
    {
        _commands.Respond(command => command.Arguments.Contains("lint"), new CommandResult(1, "src/App.tsx: 1 problem", "lint failed"));

        var result = await Npm().Run(NpmProject.CurrentDirectory, "lint", Ct);

        _commands.Executed.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            command => command.Arguments.ShouldBe(["run", "lint"]),
            command => command.WorkingDirectory.ShouldBeNull());
        result.ShouldBe(new NpmScriptResult(false, "src/App.tsx: 1 problem\nlint failed"));
    }

    [Fact]
    public async Task Install_Throws_WhenNpmCannotInstall()
    {
        _commands.Respond(command => command.Arguments.Contains("ci"), new CommandResult(1, "", "lock file out of date"));

        await Should.ThrowAsync<CommandFailedException>(() => Npm().Install(Project, Ct));
    }

    [Fact]
    public async Task EachScriptStep_RunsItsScript_AndReportsUnderItsSection()
    {
        await new NpmLint(Npm(), _report).Run(Project, Ct);
        await new NpmBuild(Npm(), _report).Run(Project, Ct);
        await new NpmTest(Npm(), _report).Run(Project, Ct);

        _commands.Executed.Select(command => command.Arguments[1]).ShouldBe(["lint", "build", "test"]);
        _report.Sections.Select(section => section.Title).ShouldBe([SectionName.Linting.Title, SectionName.Build.Title, SectionName.Tests.Title]);
    }

    [Fact]
    public async Task AFailingScript_FailsItsStep_ShowingTheEndOfItsOutput()
    {
        var output = string.Join('\n', Enumerable.Range(1, 150).Select(line => $"line {line}"));
        _commands.Respond(command => command.Arguments.Contains("test"), new CommandResult(1, output, ""));

        var result = await new NpmTest(Npm(), _report).Run(Project, Ct);

        result.IsFailure.ShouldBeTrue();
        var shown = _report.Sections.ShouldHaveSingleItem().Entries.ShouldHaveSingleItem().ShouldBeOfType<ReportParagraph>().Markdown;
        shown.ShouldContain("line 150");
        shown.ShouldContain("line 51\n");
        shown.ShouldNotContain("line 50\n");
    }

    [Fact]
    public async Task InstallStep_Installs()
    {
        (await new NpmInstall(Npm()).Run(Project, Ct)).IsFailure.ShouldBeFalse();

        _commands.Executed.ShouldHaveSingleItem().Arguments[0].ShouldBe("ci");
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
