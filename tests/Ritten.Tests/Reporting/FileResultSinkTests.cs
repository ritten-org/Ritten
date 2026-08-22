using System.Text;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;
using Ritten.Reporting.Sinks;

namespace Ritten.Tests.Reporting;

public class FileResultSinkTests
{
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IDirectory _artifacts = Substitute.For<IDirectory>();
    private readonly MemoryStream _written = new();

    public FileResultSinkTests()
    {
        var file = Substitute.For<IFile>();
        file.OpenWrite().Returns(_written);
        _artifacts.Name.Returns("artifacts");
        _artifacts.GetFile(FileResultSink.FileName).Returns(file);
        _fileSystem.Artifacts.Returns(_artifacts);
    }

    [Fact]
    public async Task WritesTheRenderedReport()
    {
        var report = new WorkflowReport("Ritten", Succeeded: true, [new ReportSection(SectionName.Tests).Success("All 12 tests passed.")]);

        await Sink().Publish(report, TestContext.Current.CancellationToken);

        var written = Encoding.UTF8.GetString(_written.ToArray());
        written.ShouldContain("Ritten");
        written.ShouldContain("All 12 tests passed.");
    }

    [Fact]
    public async Task WritesAFailedReportToo()
    {
        // The report matters most when the run didn't work.
        var report = new WorkflowReport("Ritten", Succeeded: false, [new ReportSection(SectionName.Build).Failure("The solution failed to build.")]);

        await Sink().Publish(report, TestContext.Current.CancellationToken);

        Encoding.UTF8.GetString(_written.ToArray()).ShouldContain("The solution failed to build.");
    }

    [Fact]
    public async Task CreatesTheArtifactsDirectoryFirst()
    {
        // Nothing else need have run: a job whose first step fails still leaves its report.
        await Sink().Publish(new WorkflowReport("Ritten", Succeeded: false, []), TestContext.Current.CancellationToken);

        _artifacts.Received().Create();
    }

    private FileResultSink Sink() =>
        new(Substitute.For<IWorkflowLog>(), new MarkdownReportRenderer(), _fileSystem);
}
