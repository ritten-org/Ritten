using Microsoft.Extensions.DependencyInjection;
using Ritten.CommandLine;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Engine.Workflows;
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
    public async Task Run_ReadsTheValuesTheJobDeclares()
    {
        // The engine never learns what a release is: the job names the argument and alone sees
        // the value, which arrives already read into the type the declaration chose.
        WriteRittenJson("""{ "workflow": "test" }""");
        JobArguments? received = null;
        var exitCode = await Run(
            new TestJob(arguments: [Release], configure: (_, args) => received = args),
            Given(Release, new Uri("https://releases.example/1.2.0")));

        exitCode.ShouldBe(ExitCode.Success);
        received.ShouldNotBeNull().Get(Release).ShouldBe(new Uri("https://releases.example/1.2.0"));
    }

    [Fact]
    public async Task Run_LeavesAnOmittedValueUnread()
    {
        WriteRittenJson("""{ "workflow": "test" }""");
        JobArguments? received = null;
        var exitCode = await Run(new TestJob(arguments: [Release], configure: (_, args) => received = args), JobArguments.None);

        exitCode.ShouldBe(ExitCode.Success);
        received.ShouldNotBeNull().Get(Release).ShouldBeNull();
    }

    [Fact]
    public async Task Run_RefusesAMissingRequiredValue()
    {
        WriteRittenJson("""{ "workflow": "test" }""");

        var exitCode = await Run(new TestJob(arguments: [RequiredRelease]), JobArguments.None);

        exitCode.ShouldBe(ExitCode.ConfigurationError);
    }

    /// <summary>An argument whose text only the domain that declared it knows how to read.</summary>
    private static JobArgument<Uri> Release { get; } = JobArgument.Value(
        "release",
        "Which release.",
        text => Uri.TryCreate($"https://releases.example/{text}", UriKind.Absolute, out var uri) && !text.Contains(' ')
            ? new Result<Uri>(uri)
            : Result.Error($"'{text}' is not a release."));

    private static JobArgument<Uri> RequiredRelease { get; } = JobArgument.Value(
        "release",
        "Which release.",
        text => new Result<Uri>(new Uri($"https://releases.example/{text}")),
        required: true);

    /// <summary>The values a front end would have read, as the job will be handed them.</summary>
    private static JobArguments Given<T>(JobArgument<T> argument, T value) =>
        new(new Dictionary<JobArgument, object?> { [argument] = value });

    private async Task<ExitCode> Run(TestJob job, JobArguments arguments, StepProbe? probe = null)
    {
        var builder = WorkflowApplication.CreateBuilder();
        builder.Workflows.Add(new TestWorkflow(jobs: [job]));
        builder.Services.AddSingleton(probe ?? new StepProbe());
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        var application = builder.Build().Value.ShouldNotBeNull();

        var args = new RunJobArgs(job.Name) { Directory = _root, Arguments = arguments };
        return await application.Run(args, Empty, TestContext.Current.CancellationToken);
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
