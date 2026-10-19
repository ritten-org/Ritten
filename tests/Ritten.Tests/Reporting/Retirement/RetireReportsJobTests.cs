using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Reporting;
using Ritten.Reporting.Retirement;
using Ritten.Tests.Support;

namespace Ritten.Tests.Reporting.Retirement;

public class RetireReportsJobTests
{
    [Fact]
    public async Task AHostRunsItAfterItsSubjectsToRetireTheRest()
    {
        var comments = Substitute.For<IPublishedReports>();
        comments.List(Arg.Any<CancellationToken>()).Returns([new ReportKey("Workflow", "gone")]);
        var job = new RetireReportsJob();
        var workflow = new TestWorkflow(jobs: [job]);
        var builder = WorkflowApplication.CreateBuilder();
        builder.Workflows.Add(workflow);
        builder.Services.AddSingleton(comments);
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        using var application = builder.Build().Value.ShouldNotBeNull();

        var exitCode = await application.Run(
            workflow, job, new RetireReportsArguments { Reported = new ReportSubjects(["kept"]) }, Path.GetTempPath(), new RunOptions(),
            TestContext.Current.CancellationToken);

        exitCode.ShouldBe(ExitCode.Success);
        await comments.Received().Delete(new ReportKey("Workflow", "gone"), Arg.Any<CancellationToken>());
    }
}
