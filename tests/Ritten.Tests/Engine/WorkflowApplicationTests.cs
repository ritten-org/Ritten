using Microsoft.Extensions.DependencyInjection;
using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Engine.Workflows;
using Ritten.GitHub;
using Ritten.Releases;
using Ritten.Reporting;
using Ritten.Tests.Support;
using Ritten.Workflows;
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
        builder.AddDotNetWorkflows();
        builder.Runtimes.Add<GitHubActionsRuntime>();

        builder.Build().IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Build_NamesEveryServiceAStepNeedsThatNothingRegisters()
    {
        // A workflow added without the clients its steps use is refused before any job can start.
        var builder = WorkflowApplication.CreateBuilder();
        builder.Workflows.Add<DotNetToolWorkflow>();

        var application = builder.Build();

        application.Errors.ShouldNotBeNull().ShouldContain(e => e.Message == "'read projects' needs a IDotNet, which no service registers.");
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
        // The engine never learns what a release is: the arguments name the option, and the value
        // arrives already read into the type they chose.
        WriteRittenJson("""{ "workflow": "test" }""");
        var probe = new StepProbe();
        var job = new TestJob<VersionArguments>(steps: [Step.FromType<RecordsVersion>()]);
        var application = Application(new TestWorkflow(jobs: [job]), probe);
        var version = ((IJob)job).Options.ShouldHaveSingleItem();

        var exitCode = await Run(application, "verify", options: new Dictionary<JobOption, object?> { [version] = version.Read("1.2.0").Value });

        exitCode.ShouldBe(ExitCode.Success);
        probe.Ran.ShouldBe(["1.2.0"]);
    }

    [Fact]
    public async Task Run_LeavesAnOmittedValueAtItsDefault()
    {
        WriteRittenJson("""{ "workflow": "test" }""");
        var probe = new StepProbe();
        var application = Application(new TestWorkflow(jobs: [new TestJob<VersionArguments>(steps: [Step.FromType<RecordsVersion>()])]), probe);

        var exitCode = await Run(application, "verify");

        exitCode.ShouldBe(ExitCode.Success);
        probe.Ran.ShouldBe(["none"]);
    }

    [Fact]
    public void Build_RefusesACommandLineValueThatIsRequired()
    {
        // A value the command line may leave out can't also be one the job can't run without.
        var builder = WorkflowApplication.CreateBuilder();
        builder.Workflows.Add(new TestWorkflow(jobs: [new TestJob<RequiredVersionArguments>()]));

        var application = builder.Build();

        application.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("can't be required");
    }

    [Fact]
    public async Task Run_RunsAJobWithTheArgumentsItsCallerGives()
    {
        // A host that knows what to run hands the job its arguments directly: no project file, no command line.
        var probe = new StepProbe();
        var job = new TestJob<VersionArguments>(steps: [Step.FromType<RecordsVersion>()], requiresProject: true);
        var workflow = new TestWorkflow(jobs: [job]);
        var application = Application(workflow, probe);

        var exitCode = await application.Run(
            workflow,
            job,
            new VersionArguments { Version = new RequestedVersion(NuGetVersion.Parse("2.0.0")) },
            _root,
            new RunOptions(),
            TestContext.Current.CancellationToken);

        exitCode.ShouldBe(ExitCode.Success);
        probe.Ran.ShouldBe(["2.0.0"]);
    }

    [Fact]
    public async Task Run_RefusesArgumentsOfAnotherType()
    {
        var job = new TestJob<VersionArguments>();
        var workflow = new TestWorkflow(jobs: [job]);
        var application = Application(workflow);

        var exitCode = await application.Run(workflow, job, NoArguments.Instance, _root, new RunOptions(), TestContext.Current.CancellationToken);

        exitCode.ShouldBe(ExitCode.ConfigurationError);
    }

    [Fact]
    public async Task Run_RefusesAJobOfAnotherWorkflow()
    {
        var workflow = new TestWorkflow();
        var application = Application(workflow);

        var exitCode = await application.Run(workflow, new TestJob(), new DotNetToolArguments(), _root, new RunOptions(), TestContext.Current.CancellationToken);

        exitCode.ShouldBe(ExitCode.ConfigurationError);
    }

    [Fact]
    public void Build_ResolvesAWorkflowRegisteredByTypeFromTheServices()
    {
        // A workflow added by type is built by the application's services, so it can take what they hold.
        var builder = WorkflowApplication.CreateBuilder();
        builder.Workflows.Add<NamedByService>();
        builder.Services.AddSingleton(new WorkflowName("from-services"));

        using var application = builder.Build().Value.ShouldNotBeNull();

        application.Workflows.ShouldHaveSingleItem().Name.ShouldBe("from-services");
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
        var probe = new StepProbe();
        var builder = WorkflowApplication.CreateBuilder();
        builder.Workflows.Add(new TestWorkflow("indifferent", [new TestJob(name: "init", requiresProject: false)]));
        builder.Workflows.Add(new TestWorkflow("specific", [
            new TestJob(name: "init", requiresProject: false, steps: [Step.FromType<RecordsSelection>()])
        ], recognises: "it packs as a tool"));
        builder.Services.AddSingleton(probe);
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        var application = builder.Build().Value.ShouldNotBeNull();

        var exitCode = await Run(application, "init");

        exitCode.ShouldBe(ExitCode.Success);
        probe.Ran.ShouldBe(["specific: it packs as a tool"]);
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

    /// <summary>
    /// The whole path a command line takes: resolve what the directory asks for, then run the job
    /// against it.
    /// </summary>
    private async Task<ExitCode> Run(
        WorkflowApplication application,
        string job,
        string? workflow = null,
        IReadOnlyDictionary<JobOption, object?>? options = null)
    {
        var ct = TestContext.Current.CancellationToken;
        var selection = await application.SelectWorkflow(_root, workflow, ct);
        return await application.Run(selection, new RunJobArgs(job) { Options = options ?? new Dictionary<JobOption, object?>() }, ct);
    }

    private static WorkflowApplication Application(IWorkflow workflow, StepProbe? probe = null)
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

    private sealed record VersionArguments
    {
        [CommandLineOption("version", "Which version.")]
        public RequestedVersion Version { get; init; } = RequestedVersion.None;
    }

    private sealed record RequiredVersionArguments
    {
        [CommandLineOption("version", "Which version.")]
        public required RequestedVersion Version { get; init; }
    }

    private sealed record WorkflowName(string Value);

    private sealed class NamedByService(WorkflowName name) : IWorkflow
    {
        public string Name => name.Value;

        public string Label => name.Value;

        public IReadOnlyList<IJob> Jobs => [];
    }

    [Step("records version", StepKind.Work)]
    private sealed class RecordsVersion(StepProbe probe)
    {
        public StepResult Run(RequestedVersion version)
        {
            probe.Ran.Add(version.Version?.ToString() ?? "none");
            return StepResult.Successful;
        }
    }

    [Step("records selection", StepKind.Work)]
    private sealed class RecordsSelection(StepProbe probe, SelectedWorkflow selected)
    {
        public StepResult Run()
        {
            probe.Ran.Add($"{selected.Workflow.Name}: {selected.Recognised}");
            return StepResult.Successful;
        }
    }
}
