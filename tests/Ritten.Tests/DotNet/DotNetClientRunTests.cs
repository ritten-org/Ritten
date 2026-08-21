using System.Text;
using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Tests.Support;

namespace Ritten.Tests.DotNet;

/// <summary>
/// Tests for the <see cref="DotNetClient"/> operations that run dotnet commands and interpret
/// their output: <c>Restore</c>, <c>Build</c>, <c>Test</c>, and <c>Format</c>.
/// </summary>
public class DotNetClientRunTests
{
    private readonly FakeCommandRunner _commands = new();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly DotNetClient _client;

    public DotNetClientRunTests()
    {
        _fileSystem.ProjectRoot.AbsolutePath.Returns("/repo");
        _client = new DotNetClient(_commands, _fileSystem);
    }

    private IDirectory ReportDirectory()
    {
        var reportDirectory = Substitute.For<IDirectory>();
        reportDirectory.AbsolutePath.Returns("/repo/temp/format");
        _fileSystem.Temp.GetDirectory("format").Returns(reportDirectory);
        return reportDirectory;
    }

    [Fact]
    public async Task Restore_ComposesTheCommandAndParsesTheRestoredProjects()
    {
        _commands.Respond(
            c => c.Arguments.Contains("restore"),
            new CommandResult(0, "Restored /repo/src/My.csproj (in 407 ms).\n", ""));

        var result = await _client.Restore(new RestoreArgs { Project = "My.slnx" }, TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.ShouldBe(["restore", "My.slnx"]);
        command.ThrowsOnError.ShouldBeFalse();
        result.Succeeded.ShouldBeTrue();
        result.RestoredProjects.ShouldBe(["My"]);
    }

    [Fact]
    public async Task Restore_ParsesDiagnosticsOnFailure()
    {
        _commands.Respond(
            c => c.Arguments.Contains("restore"),
            new CommandResult(1,
                "tests/My.Tests.csproj : error NU1903: Package 'SSH.NET' 2025.1.0 has a known high severity vulnerability [/repo/My.slnx]\n",
                ""));

        var result = await _client.Restore(new RestoreArgs(), TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeFalse();
        var diagnostic = result.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Code.ShouldBe("NU1903");
        diagnostic.Message.ShouldContain("SSH.NET");
    }

    [Fact]
    public async Task Pack_ComposesTheCommandAndReturnsThePackages()
    {
        var package = Substitute.For<IFile>();
        var output = Substitute.For<IDirectory>();
        output.AbsolutePath.Returns("/repo/artifacts");
        output.GetFiles("*.nupkg").Returns([package]);

        var result = await _client.Pack(
            new PackArgs { Project = "src/My.csproj", Configuration = "Release", NoBuild = true, Output = output },
            TestContext.Current.CancellationToken);

        output.Received().Create();
        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.ShouldBe(["pack", "src/My.csproj", "--no-build", "--configuration", "Release", "--output", "/repo/artifacts"]);
        command.ThrowsOnError.ShouldBeTrue();
        result.Packages.ShouldBe([package]);
    }

    [Fact]
    public async Task Build_ComposesTheCommandAndParsesDiagnostics()
    {
        _commands.Respond(
            c => c.Arguments.Contains("build"),
            new CommandResult(1, "Program.cs(1,2): error CS0103: The name 'x' does not exist\n", ""));

        var result = await _client.Build(
            new BuildArgs { Configuration = "Release", NoRestore = true },
            TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments
            .ShouldBe(["build", "--no-restore", "--configuration", "Release"]);
        result.Succeeded.ShouldBeFalse();
        result.Diagnostics.ShouldHaveSingleItem().Code.ShouldBe("CS0103");
    }

    [Fact]
    public async Task Build_SucceedsWithNoDiagnostics()
    {
        var result = await _client.Build(new BuildArgs(), TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments.ShouldBe(["build"]);
        result.Succeeded.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Test_AggregatesEveryTrxFileTheRunProduced()
    {
        var firstTrx = TrxFile(passed: 2, failed: 1, failure: "Suite.One");
        var secondTrx = TrxFile(passed: 3, failed: 1, failure: "Suite.Two");
        var resultsDirectory = Substitute.For<IDirectory>();
        resultsDirectory.AbsolutePath.Returns("/repo/temp/test-results");
        resultsDirectory.GetFiles("*.trx").Returns([firstTrx, secondTrx]);
        _commands.Respond(c => c.Arguments.Contains("test"), new CommandResult(1, "", ""));

        var result = await _client.Test(
            new TestArgs { Configuration = "Release", NoBuild = true, ResultsDirectory = resultsDirectory },
            TestContext.Current.CancellationToken);

        resultsDirectory.Received().Create();
        _commands.Executed.ShouldHaveSingleItem().Arguments.ShouldBe(
            ["test", "--no-build", "--configuration", "Release", "--report-trx", "--results-directory", "/repo/temp/test-results"]);
        result.Succeeded.ShouldBeFalse();
        result.Passed.ShouldBe(5);
        result.Failed.ShouldBe(2);
        result.Failures.Select(f => f.TestName).ShouldBe(["Suite.One", "Suite.Two"]);
    }

    [Fact]
    public async Task Test_CollectsCoverageUnderTheNameTheCoverageStepLooksFor()
    {
        // The platform writes wherever it is told, and ReadCoverage globs for coverage.cobertura.xml, so
        // the two have to agree on the name.
        var resultsDirectory = Substitute.For<IDirectory>();
        resultsDirectory.AbsolutePath.Returns("/repo/temp/test-results");
        resultsDirectory.GetFiles("*.trx").Returns([]);

        await _client.Test(
            new TestArgs { ResultsDirectory = resultsDirectory, CollectCoverage = true },
            TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments.ShouldBe(
        [
            "test", "--report-trx", "--results-directory", "/repo/temp/test-results",
            "--coverage", "--coverage-output-format", "cobertura", "--coverage-output", "coverage.cobertura.xml"
        ]);
    }

    [Fact]
    public async Task Format_ReadsTheReportWhenVerifyingRefusesTheSolution()
    {
        var reportDirectory = ReportDirectory();
        var reportFile = FileWithContent("""[{"FilePath": "/repo/src/B.cs"}, {"FilePath": "/repo/src/A.cs"}]""");
        reportFile.Exists.Returns(true);
        reportDirectory.GetFile("format-report.json").Returns(reportFile);
        _commands.Respond(c => c.Arguments.Contains("format"), new CommandResult(2, "", ""));

        var result = await _client.Format(new FormatArgs { VerifyNoChanges = true }, TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments.ShouldBe(
            ["format", "whitespace", "--verify-no-changes", "--report", "/repo/temp/format"]);
        result.Succeeded.ShouldBeFalse();
        result.UnformattedFiles.ShouldBe(["src/A.cs", "src/B.cs"]);
        reportDirectory.Received().Delete();
    }

    [Fact]
    public async Task Format_ReportsTheFilesItRewrote()
    {
        // Formatting for real is the same command without --verify-no-changes, and the report
        // then names what it changed rather than what it refused.
        var reportDirectory = ReportDirectory();
        var reportFile = FileWithContent("""[{"FilePath": "/repo/src/A.cs"}]""");
        reportFile.Exists.Returns(true);
        reportDirectory.GetFile("format-report.json").Returns(reportFile);

        var result = await _client.Format(new FormatArgs(), TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments.ShouldBe(
            ["format", "whitespace", "--report", "/repo/temp/format"]);
        result.Succeeded.ShouldBeTrue();
        result.UnformattedFiles.ShouldBe(["src/A.cs"]);
        reportDirectory.Received().Delete();
    }

    [Fact]
    public async Task Format_ReportsNothingWhenEverythingIsFormatted()
    {
        var reportDirectory = ReportDirectory();

        var result = await _client.Format(new FormatArgs { VerifyNoChanges = true }, TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeTrue();
        result.UnformattedFiles.ShouldBeEmpty();
        reportDirectory.Received().Delete();
    }

    private static IFile TrxFile(int passed, int failed, string failure)
    {
        return FileWithContent(
            $"""
             <?xml version="1.0" encoding="utf-8"?>
             <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
               <Results>
                 <UnitTestResult testName="{failure}" outcome="Failed">
                   <Output><ErrorInfo><Message>failed</Message></ErrorInfo></Output>
                 </UnitTestResult>
               </Results>
               <ResultSummary><Counters passed="{passed}" failed="{failed}" notExecuted="0" /></ResultSummary>
             </TestRun>
             """);
    }

    private static IFile FileWithContent(string content)
    {
        var file = Substitute.For<IFile>();
        file.OpenRead().Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(content)));
        return file;
    }
}
