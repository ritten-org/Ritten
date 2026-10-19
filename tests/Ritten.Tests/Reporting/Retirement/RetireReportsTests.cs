using Ritten.Contracts;
using Ritten.Reporting;
using Ritten.Reporting.Retirement;

namespace Ritten.Tests.Reporting.Retirement;

public class RetireReportsTests
{
    private readonly IPublishedReports _comments = Substitute.For<IPublishedReports>();

    [Fact]
    public async Task RemovesOnlyTheWorkflowsSubjectsThatWereNotReported()
    {
        _comments.List(Arg.Any<CancellationToken>()).Returns([
            new ReportKey("CI"),
            new ReportKey("CI", "caddy/infrastructure"),
            new ReportKey("CI", "dns/infrastructure"),
            new ReportKey("Release", "dns/infrastructure")
        ]);

        var result = await Step().Run(new ReportSubjects(["caddy/infrastructure"]), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _comments.Received(1).Delete(Arg.Any<ReportKey>(), Arg.Any<CancellationToken>());
        await _comments.Received().Delete(new ReportKey("CI", "dns/infrastructure"), TestContext.Current.CancellationToken);
    }

    private RetireReports Step() => new([_comments], new RunContext { Title = "CI" }, Substitute.For<IWorkflowLog>());
}
