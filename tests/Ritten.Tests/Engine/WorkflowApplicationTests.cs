using Microsoft.Extensions.DependencyInjection;
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

        var exitCode = await Run(application, "verify");

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

        return await Run(application, job.Name, arguments: arguments);
    }

    [Fact]
    public async Task Run_ReportsAJobTheWorkflowDoesNotDeclare()
    {
        WriteRittenJson("""{ "workflow": "test" }""");
        var application = Application(new TestWorkflow());

        var exitCode = await Run(application, "deploy");

        exitCode.ShouldBe(ExitCode.ConfigurationError);
    }

    [Fact]
    public async Task Run_ReportsAWorkflowTheApplicationDoesNotKnow()
    {
        WriteRittenJson("""{ "workflow": "imaginary" }""");
        var application = Application(new TestWorkflow());

        var exitCode = await Run(application, "verify");

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

        var exitCode = await Run(application, "verify");

        exitCode.ShouldBe(ExitCode.Success);
        probe.Ran.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Run_RunsAJobThatNeedsNoProjectWithoutOne()
    {
        // Nothing has been set up yet: the job that does the setting up is told which workflow to
        // run, and reads the settings it would have loaded as their defaults.
        var probe = new StepProbe();
        var application = Application(new TestWorkflow(jobs:
            [new TestJob(name: "init", steps: [Step.FromType<ProbeStep>()], requiresProject: false)]), probe);

        var exitCode = await Run(application, "init", workflow: "test");

        exitCode.ShouldBe(ExitCode.Success);
        probe.Ran.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Run_JudgesNoSettingsForAJobThatNeedsNoProject()
    {
        // There is nothing to judge in settings nobody has written: the job that writes them
        // can't be refused for their not being there.
        var job = new TestJob(name: "init", requiresProject: false, validate: settings => settings.Require(s => s.Build.Project));
        var application = Application(new TestWorkflow(jobs: [job]));

        var exitCode = await Run(application, "init", workflow: "test");

        exitCode.ShouldBe(ExitCode.Success);
    }

    [Fact]
    public async Task Run_LetsAJobThatNeedsNoProjectFinishAHalfWrittenOne()
    {
        // A project file that exists but declares nothing is exactly what init is for; every
        // other job still gets told the declaration is missing.
        WriteRittenJson("""{ "build": { "project": "src/Thing/Thing.csproj" } }""");
        var probe = new StepProbe();
        var application = Application(new TestWorkflow(jobs:
            [new TestJob(name: "init", steps: [Step.FromType<ProbeStep>()], requiresProject: false)]), probe);

        var exitCode = await Run(application, "init", workflow: "test");

        exitCode.ShouldBe(ExitCode.Success);
        probe.Ran.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Run_ReportsAProjectThatDeclaresNoWorkflowForAJobThatNeedsOne()
    {
        WriteRittenJson("""{ "build": { "project": "src/Thing/Thing.csproj" } }""");
        var application = Application(new TestWorkflow(jobs: [new TestJob()], recognises: "there's a project here"));

        var exitCode = await Run(application, "verify");

        exitCode.ShouldBe(ExitCode.ConfigurationError);
    }

    [Fact]
    public async Task Run_ReportsTheMissingProjectForAJobThatNeedsOne()
    {
        var application = Application(new TestWorkflow(jobs: [new TestJob()], recognises: "there's a project here"));

        var exitCode = await Run(application, "verify");

        exitCode.ShouldBe(ExitCode.ConfigurationError);
    }

    [Fact]
    public async Task Run_RecognisesTheWorkflowWhenNothingDeclaresOne()
    {
        // Registration order is precedence: the first workflow to recognise the repository wins,
        // and what it recognised is handed to the run so the job can say why it's doing this.
        SelectedWorkflow? selected = null;
        var builder = WorkflowApplication.CreateBuilder();
        builder.Workflows.Add(new TestWorkflow("indifferent", [new TestJob(name: "init", requiresProject: false)]));
        builder.Workflows.Add(new TestWorkflow("specific", [
            new TestJob(name: "init", requiresProject: false, configure: (b, _) => selected = Selected(b))
        ], recognises: "it packs as a tool"));
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        var application = builder.Build().Value.ShouldNotBeNull();

        var exitCode = await Run(application, "init");

        exitCode.ShouldBe(ExitCode.Success);
        selected.ShouldNotBeNull().Workflow.Name.ShouldBe("specific");
        selected.Recognised.ShouldBe("it packs as a tool");
    }

    [Fact]
    public async Task Resolve_RefusesANameTheProjectContradicts()
    {
        // A repository that has declared its workflow has settled the question, so a name that
        // disagrees is a mistaken belief worth reporting — never quietly discarded.
        WriteRittenJson("""{ "workflow": "declared" }""");
        var builder = WorkflowApplication.CreateBuilder();
        builder.Workflows.Add(new TestWorkflow("declared", [new TestJob(name: "init", requiresProject: false)]));
        builder.Workflows.Add(new TestWorkflow("named", [new TestJob(name: "init", requiresProject: false)]));
        var application = builder.Build().Value.ShouldNotBeNull();

        var selection = await application.SelectWorkflow(_root, "named", TestContext.Current.CancellationToken);

        selection.IsError.ShouldBeTrue();
        selection.Errors.First().Message.ShouldContain("'named' can't be run here");
    }

    [Fact]
    public async Task Resolve_TakesTheNameWhenItAgreesWithTheProject()
    {
        WriteRittenJson("""{ "workflow": "declared" }""");
        var builder = WorkflowApplication.CreateBuilder();
        builder.Workflows.Add(new TestWorkflow("declared", [new TestJob(name: "init", requiresProject: false)]));
        var application = builder.Build().Value.ShouldNotBeNull();

        var selection = await application.SelectWorkflow(_root, "declared", TestContext.Current.CancellationToken);

        selection.IsSuccess.ShouldBeTrue();
        selection.Value.ShouldNotBeNull().Recognised.ShouldBeNull();
    }

    [Fact]
    public async Task Run_ReportsAWorkflowNameNobodyKnows()
    {
        var application = Application(new TestWorkflow(jobs: [new TestJob(name: "init", requiresProject: false)]));

        var exitCode = await Run(application, "init", workflow: "imaginary");

        exitCode.ShouldBe(ExitCode.ConfigurationError);
    }

    /// <summary>What the run was assembled for, read back out of the registrations it made.</summary>
    private static SelectedWorkflow? Selected(IWorkflowBuilder builder) => builder.Services
        .FirstOrDefault(service => service.ServiceType == typeof(SelectedWorkflow))?.ImplementationInstance as SelectedWorkflow;

    /// <summary>
    /// The whole path a command line takes: resolve what the directory asks for, then run the job
    /// against it.
    /// </summary>
    private async Task<ExitCode> Run(WorkflowApplication application, string job, string? workflow = null, JobArguments? arguments = null)
    {
        var ct = TestContext.Current.CancellationToken;
        var selection = await application.SelectWorkflow(_root, workflow, ct);
        return await application.Run(selection, new RunJobArgs(job) { Arguments = arguments ?? JobArguments.None }, Empty, ct);
    }

    private static Func<string, string?> Empty { get; } = _ => null;

    private static WorkflowApplication Application(TestWorkflow workflow, StepProbe? probe = null)
    {
        var builder = WorkflowApplication.CreateBuilder();
        builder.Workflows.Add(workflow);
        builder.Services.AddSingleton(probe ?? new StepProbe());
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        return builder.Build().Value.ShouldNotBeNull();
    }

    private void WriteRittenJson(string content)
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "ritten.json"), content);
    }
}
