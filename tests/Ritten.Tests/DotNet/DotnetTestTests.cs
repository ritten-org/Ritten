using Microsoft.Extensions.Options;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Reporting;
using Ritten.Tests.Support;
using TestResult = Ritten.DotNet.TestResult;

namespace Ritten.Tests.DotNet;

public class DotnetTestTests
{
    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();
    private readonly IDotNet _dotnet = Substitute.For<IDotNet>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IWorkflowReport _report = Substitute.For<IWorkflowReport>();
    private readonly ReportSection _section = new("Tests");

    public DotnetTestTests()
    {
        _report.Section("Tests").Returns(_section);
        _fileSystem.Temp.GetDirectory("test-results").Returns(Substitute.For<IDirectory>());
    }

    [Fact]
    public async Task ContinuesAndReportsTheCountsWhenTheTestsPass()
    {
        Respond(new TestResult { Succeeded = true, Passed = 5, Failed = 0, Skipped = 0 });

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        _section.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("**5** tests passed");
    }

    [Fact]
    public async Task ListsTheIndividualFailuresWhenTestsFail()
    {
        Respond(new TestResult
        {
            Succeeded = false,
            Passed = 3,
            Failed = 1,
            Skipped = 0,
            Failures = [new TestFailure("My.Tests.Boom", "Expected true but was false")]
        });

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().Select(e => e.Message).ShouldBe([
            "1 test failed (3 passed, 0 skipped):",
            "My.Tests.Boom: Expected true but was false"
        ]);
    }

    [Fact]
    public async Task ReportsTheCommandOutputWhenTheRunFailsWithoutResults()
    {
        // The run itself broke before any test reported — the command's output is the diagnosis,
        // and it must not take a --verbose re-run to see it.
        Respond(new TestResult
        {
            Succeeded = false,
            Passed = 0,
            Failed = 0,
            Skipped = 0,
            FailureOutput = ["error: unknown option: --report-trx"]
        });

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().Select(e => e.Message).ShouldBe([
            "`dotnet test` failed before reporting any results:",
            "error: unknown option: --report-trx"
        ]);
        _section.Tone.ShouldBe(ReportTone.Failure);
        _section.Entries[1].ToMarkdown().ShouldContain("unknown option");
    }

    [Fact]
    public async Task StillAdvisesAVerboseRunWhenThereIsNoOutputToShow()
    {
        Respond(new TestResult { Succeeded = false, Passed = 0, Failed = 0, Skipped = 0 });

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("--verbose");
    }

    private void Respond(TestResult result) =>
        _dotnet.Test(Arg.Any<TestArgs>(), Arg.Any<CancellationToken>()).Returns(result);

    private DotnetTest Step() =>
        new(_log, Options.Create(TestOptions.DotNet()), _fileSystem, _dotnet, _report);
}
