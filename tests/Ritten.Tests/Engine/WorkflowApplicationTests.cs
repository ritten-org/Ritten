using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.GitHub;
using Ritten.Reporting;
using Ritten.Tests.Support;
using Ritten.Workflows.DotNetTool;

namespace Ritten.Tests.Engine;

public class WorkflowApplicationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ritten-application-{Guid.NewGuid():N}");

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Fact]
    public void CreateBuilder_RegistersByType()
    {
        var builder = WorkflowApplication.CreateBuilder();
        builder.Workflows.Add<DotNetToolWorkflow>();
        builder.Runtimes.Add<GitHubActionsRuntime>();

        builder.Build().IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Build_JudgesTheWholeRegisteredModel()
    {
        var builder = WorkflowApplication.CreateBuilder();
        builder.Workflows.Add(new TestWorkflow("same"));
        builder.Workflows.Add(new TestWorkflow("same"));

        var application = builder.Build();

        application.IsError.ShouldBeTrue();
        application.Errors.ShouldHaveSingleItem().Message.ShouldBe("Two workflows are registered under the name 'same'.");
    }

    [Fact]
    public async Task Run_RunsTheDeclaredJobWithTheSharedServices()
    {
        // The probe reaches the step only through the application's shared services, so this run
        // proves the whole path: resolve the project, select workflow and job, assemble, execute.
        WriteRittenJson("""{ "workflow": "test" }""");
        var probe = new StepProbe();
        var builder = WorkflowApplication.CreateBuilder();
        builder.Workflows.Add(new TestWorkflow(jobs: [new TestJob(steps: [Step.FromType<ProbeStep>()])]));
        builder.Services.AddSingleton(probe);
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        var application = builder.Build().Value.ShouldNotBeNull();

        var exitCode = await application.Run(new RunJobArgs("verify") { Directory = _root }, Empty, TestContext.Current.CancellationToken);

        exitCode.ShouldBe(ExitCode.Success);
        probe.Ran.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Run_ReportsAJobTheWorkflowDoesNotDeclare()
    {
        WriteRittenJson("""{ "workflow": "test" }""");
        var application = Application(new TestWorkflow());

        var exitCode = await application.Run(new RunJobArgs("deploy") { Directory = _root }, Empty, TestContext.Current.CancellationToken);

        exitCode.ShouldBe(ExitCode.ConfigurationError);
    }

    [Fact]
    public async Task Run_ReportsAWorkflowTheApplicationDoesNotKnow()
    {
        WriteRittenJson("""{ "workflow": "imaginary" }""");
        var application = Application(new TestWorkflow());

        var exitCode = await application.Run(new RunJobArgs("verify") { Directory = _root }, Empty, TestContext.Current.CancellationToken);

        exitCode.ShouldBe(ExitCode.ConfigurationError);
    }

    [Fact]
    public async Task Run_ResolvesTheProjectFileTheHostRenamed()
    {
        // The host names the file on the application builder; nothing else changes shape.
        Directory.CreateDirectory(_root);
        await File.WriteAllTextAsync(Path.Combine(_root, "build.json"), """{ "workflow": "test" }""", TestContext.Current.CancellationToken);
        var probe = new StepProbe();
        var builder = WorkflowApplication.CreateBuilder();
        builder.ProjectFileName = "build.json";
        builder.Workflows.Add(new TestWorkflow(jobs: [new TestJob(steps: [Step.FromType<ProbeStep>()])]));
        builder.Services.AddSingleton(probe);
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        var application = builder.Build().Value.ShouldNotBeNull();

        var exitCode = await application.Run(new RunJobArgs("verify") { Directory = _root }, Empty, TestContext.Current.CancellationToken);

        exitCode.ShouldBe(ExitCode.Success);
        probe.Ran.ShouldHaveSingleItem();
    }

    private static Func<string, string?> Empty { get; } = _ => null;

    private static WorkflowApplication Application(TestWorkflow workflow)
    {
        var builder = WorkflowApplication.CreateBuilder();
        builder.Workflows.Add(workflow);
        return builder.Build().Value.ShouldNotBeNull();
    }

    private void WriteRittenJson(string content)
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "ritten.json"), content);
    }
}
