using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Reporting;

namespace Ritten.Tests.DotNet;

public class DotnetRestoreTests
{
    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();
    private readonly IDotNet _dotnet = Substitute.For<IDotNet>();
    private readonly IBuildReport _report = Substitute.For<IBuildReport>();
    private readonly ReportSection _section = new("Restore");

    public DotnetRestoreTests()
    {
        _report.Section("Restore").Returns(_section);
    }

    [Fact]
    public async Task ContinuesWithoutTouchingTheReportWhenTheRestoreSucceeds()
    {
        Respond(new RestoreResult { Succeeded = true, RestoredProjects = ["My"] });

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        _section.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReportsTheDiagnosticsWhenTheRestoreFails()
    {
        Respond(new RestoreResult
        {
            Succeeded = false,
            Diagnostics =
            [
                new DotNetDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Code = "NU1903",
                    Message = "Package 'SSH.NET' 2025.1.0 has a known high severity vulnerability"
                }
            ]
        });

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("NU1903");
        _section.Tone.ShouldBe(ReportTone.Failure);
        _section.Entries.Count.ShouldBe(2);
        _section.Entries[1].ToMarkdown().ShouldContain("NU1903");
    }

    [Fact]
    public async Task PointsAtVerboseWhenTheFailureHasNoDiagnostics()
    {
        Respond(new RestoreResult { Succeeded = false });

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("--verbose");
        _section.Tone.ShouldBe(ReportTone.Failure);
    }

    private void Respond(RestoreResult result) =>
        _dotnet.Restore(Arg.Any<RestoreArgs>(), Arg.Any<CancellationToken>()).Returns(result);

    private DotnetRestore Step() => new(_log, _dotnet, _report);
}
