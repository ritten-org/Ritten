using System.Text;
using Ritten.CodeCoverage.Steps;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Tests.CodeCoverage;

public class ReadCoverageTests
{
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    [Fact]
    public void CombinesEveryReportTheTestsProduced()
    {
        SetReports(
            """<coverage lines-covered="10" lines-valid="20" branches-covered="1" branches-valid="2" />""",
            """<coverage lines-covered="30" lines-valid="20" branches-covered="3" branches-valid="2" />""");

        var result = Step().Run();

        result.Outcome.IsFailure.ShouldBeFalse();
        var coverage = result.Value.ShouldNotBeNull();
        coverage.LinesCovered.ShouldBe(40);
        coverage.LinesValid.ShouldBe(40);
    }

    [Fact]
    public void FailsWithTheFixWhenNoReportsWereProduced()
    {
        // The likeliest cause is a test project without the collector package; say so.
        SetReports();

        var result = Step().Run();

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem()
            .Message.ShouldContain("Microsoft.Testing.Extensions.CodeCoverage");
    }

    private void SetReports(params string[] reports)
    {
        var files = reports.Select(xml =>
        {
            var file = Substitute.For<IFile>();
            file.OpenRead().Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(xml)));
            return file;
        }).ToList();

        _fileSystem.Temp
            .GetDirectory("test-results")
            .GetFiles("**/coverage.cobertura.xml")
            .Returns(files);
    }

    private ReadCoverage Step() =>
        new(Substitute.For<IWorkflowLog>(), _fileSystem);
}
