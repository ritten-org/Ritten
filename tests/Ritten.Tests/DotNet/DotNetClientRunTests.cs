using System.Text;
using Hamelin;
using Hamelin.FileSystem;
using Ritten.Commands;
using Ritten.DotNet;
using Ritten.Tests.Support;

namespace Ritten.Tests.DotNet;

/// <summary>
/// Tests for the <see cref="DotNetClient"/> operations that run dotnet commands and interpret
/// their output: <c>Build</c>, <c>Test</c>, and <c>CheckFormat</c>.
/// </summary>
public class DotNetClientRunTests
{
    private readonly FakeCommandRunner _commands = new();
    private readonly IPipelineContext _context = Substitute.For<IPipelineContext>();
    private readonly DotNetClient _client;

    public DotNetClientRunTests()
    {
        _context.CurrentDirectory.Returns("/repo");
        _client = new DotNetClient(_commands, _context);
    }

    [Fact]
    public async Task Restore_ComposesTheCommandAndThrowsOnFailure()
    {
        await _client.Restore(new RestoreArgs { Project = "My.slnx" }, TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.ShouldBe(["restore", "My.slnx"]);
        command.ThrowsOnError.ShouldBeTrue();
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
            ["test", "--no-build", "--configuration", "Release", "--logger", "trx", "--results-directory", "/repo/temp/test-results"]);
        result.Succeeded.ShouldBeFalse();
        result.Passed.ShouldBe(5);
        result.Failed.ShouldBe(2);
        result.Failures.Select(f => f.TestName).ShouldBe(["Suite.One", "Suite.Two"]);
    }

    [Fact]
    public async Task CheckFormat_ReadsTheReportOnFailure()
    {
        var reportDirectory = Substitute.For<IDirectory>();
        reportDirectory.AbsolutePath.Returns("/repo/temp/format");
        var reportFile = FileWithContent("""[{"FilePath": "/repo/src/B.cs"}, {"FilePath": "/repo/src/A.cs"}]""");
        reportFile.Exists.Returns(true);
        reportDirectory.GetFile("format-report.json").Returns(reportFile);
        _commands.Respond(c => c.Arguments.Contains("format"), new CommandResult(2, "", ""));

        var result = await _client.CheckFormat(
            new FormatArgs { ReportDirectory = reportDirectory },
            TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeFalse();
        result.UnformattedFiles.ShouldBe(["src/A.cs", "src/B.cs"]);
    }

    [Fact]
    public async Task CheckFormat_SucceedsWithoutReadingTheReport()
    {
        var reportDirectory = Substitute.For<IDirectory>();
        reportDirectory.AbsolutePath.Returns("/repo/temp/format");

        var result = await _client.CheckFormat(
            new FormatArgs { ReportDirectory = reportDirectory },
            TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeTrue();
        result.UnformattedFiles.ShouldBeEmpty();
        reportDirectory.DidNotReceiveWithAnyArgs().GetFile(default!);
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
