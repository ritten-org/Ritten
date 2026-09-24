using Ritten.Contracts.FileSystem;
using Ritten.Reporting;
using Ritten.Reporting.Sinks;
using Ritten.Tests.Support;

namespace Ritten.Tests.Reporting;

public class FileResultSinkTests
{
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly MemoryDirectory _artifacts = new("/repo/artifacts");

    public FileResultSinkTests() => _fileSystem.Artifacts.Returns(_artifacts);

    private string Written() => _artifacts.File(FileResultSink.FileName).Text.ShouldNotBeNull();

    [Fact]
    public async Task WritesTheRenderedReport()
    {
        var report = new WorkflowReport("Ritten", Succeeded: true, [new ReportSection(SectionName.Tests).Success("All 12 tests passed.")]);

        await Sink().Publish(report, TestContext.Current.CancellationToken);

        var written = Written();
        written.ShouldContain("Ritten");
        written.ShouldContain("All 12 tests passed.");
    }

    [Fact]
    public async Task WritesAFailedReportToo()
    {
        // The report matters most when the run didn't work.
        var report = new WorkflowReport("Ritten", Succeeded: false, [new ReportSection(SectionName.Build).Failure("The solution failed to build.")]);

        await Sink().Publish(report, TestContext.Current.CancellationToken);

        Written().ShouldContain("The solution failed to build.");
    }

    [Fact]
    public async Task CreatesTheArtifactsDirectoryFirst()
    {
        // Nothing else need have run: a job whose first step fails still leaves its report.
        await Sink().Publish(new WorkflowReport("Ritten", Succeeded: false, []), TestContext.Current.CancellationToken);

        _artifacts.Created.ShouldBeTrue();
    }

    private FileResultSink Sink() =>
        new(Substitute.For<IWorkflowLog>(), new MarkdownReportRenderer(), _fileSystem);
}
