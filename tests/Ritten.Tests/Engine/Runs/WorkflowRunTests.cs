using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Engine.Runtimes;
using Ritten.Tests.Support;

namespace Ritten.Tests.Engine.Runs;

public class WorkflowRunTests
{
    private readonly StepProbe _probe = new();
    private readonly TestRuntime _runtime = new();

    [Fact]
    public async Task Run_WithPassingStep_ReturnsZero()
    {
        var application = Application(new TestJob(steps: [Step.FromType<ProbeStep>()]));

        var exitCode = await application.Run();

        exitCode.ShouldBe(ExitCode.Success);
        _probe.Ran.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Run_WithFailingStep_ReturnsFailure()
    {
        var application = Application(new TestJob(steps: [Step.FromType<FailingStep>()]));

        var exitCode = await application.Run();

        exitCode.ShouldBe(ExitCode.Failed);
    }

    [Fact]
    public async Task Run_ReportsEveryUnmetRequirementAtOnce()
    {
        // Being told about all of them beats fixing them one run at a time. The keys are derived
        // from the property chains, so they can't drift from the arguments they describe.
        var application = Application(new TestJob("deploy", validate: a => a.Require(x => x.Build.Project).Require(x => x.Build.Configuration)));

        var exitCode = await application.Run("deploy", settings: """{ "build": { "configuration": "" } }""");

        exitCode.ShouldBe(ExitCode.ConfigurationError);
        _runtime.Console.Errors.ShouldBe([
            "'build.project' not set in ritten.json.",
            "'build.configuration' not set in ritten.json."
        ]);
    }

    [Fact]
    public async Task Run_NamesTheHostsProjectFileInArgumentErrors()
    {
        // The error points at the file the reader actually has, whatever the host called it.
        var application = Application(new TestJob("deploy", validate: a => a.Require(x => x.Build.Project)), fileName: "build.json");

        await application.Run("deploy", fileName: "build.json");

        _runtime.Console.Errors.ShouldHaveSingleItem().ShouldBe("'build.project' not set in build.json.");
    }

    [Fact]
    public void Build_ConfiguresTheServicesOfTheDetectedRuntime()
    {
        var runtime = new StubRuntime();

        var application = TestApplication.Build([new TestJob()], runtime: runtime, environment: _ => "set");

        application.IsSuccess.ShouldBeTrue();
        application.Value.Dispose();
        // The runtime reads its claimed variables from the unfiltered environment: they're its own.
        runtime.SeenSecret.ShouldBe("set");
    }

    [Fact]
    public void Build_SuppliesTheRunFactDefaultsWhenNothingElseDoes()
    {
        // The engine's defaults keep ValidateOnBuild happy on runtimes that know nothing about
        // these facts: steps see "not a pull request" and "no labels", never a missing
        // registration.
        using var application = Application(new TestJob());
        using var scope = application.Services.CreateScope();

        scope.ServiceProvider.GetRequiredService<RunContext>().Title.ShouldBe("Workflow");
        scope.ServiceProvider.GetRequiredService<PullRequest>().IsPullRequest.ShouldBeFalse();
        scope.ServiceProvider.GetRequiredService<IPullRequestLabels>().ShouldBeOfType<NoPullRequestLabels>();
    }

    [Fact]
    public void Build_LeavesALabelReadTheHostRegistered()
    {
        // The default is only for runs where nobody knows better; anything the host or a runtime
        // declared wins over it.
        var own = Substitute.For<IPullRequestLabels>();
        using var application = Application(new TestJob(), services => services.AddSingleton(own));
        using var scope = application.Services.CreateScope();

        scope.ServiceProvider.GetRequiredService<IPullRequestLabels>().ShouldBeSameAs(own);
    }

    [Fact]
    public void Build_ReportsAServiceAStepNeedsThatNothingRegisters()
    {
        // Steps are built as a run reaches them, so the application checks what they ask for up front.
        var application = TestApplication.Build([new TestJob(steps: [Step.FromType<ProbeStep>()])]);

        application.IsError.ShouldBeTrue();
        application.Errors.ShouldHaveSingleItem().Message.ShouldBe("'probe' needs a StepProbe, which no service registers.");
    }

    [Fact]
    public async Task Run_HidesClaimedVariablesFromArgumentValidation()
    {
        // The variable exists in the process environment, but the runtime consumed it — so a job
        // requiring it fails loudly instead of running with a value that belongs to the runtime.
        var runtime = new StubRuntime();
        var application = TestApplication.Create([new TestJob(validate: a => a.RequireEnvironment("STUB_SECRET"))], runtime: runtime, environment: _ => "set");

        var exitCode = await application.Run();

        exitCode.ShouldBe(ExitCode.ConfigurationError);
        runtime.Console.Errors.ShouldHaveSingleItem().ShouldBe("STUB_SECRET is not set.");
    }

    [Fact]
    public async Task Run_RunsEachJobInAScopeOfItsOwn()
    {
        // What one run produced or registered is gone before the next starts: the application outlives its runs.
        var application = Application(new TestJob(steps: [Step.FromType<ProbeStep>()]));

        await application.Run();
        await application.Run();

        _probe.Ran.Count.ShouldBe(2);
    }

    private WorkflowApplication Application(TestJob job, Action<IServiceCollection>? services = null, string fileName = RittenProject.DefaultFileName) =>
        TestApplication.Create([job], builder =>
        {
            builder.Services.AddSingleton(_probe);
            services?.Invoke(builder.Services);
        }, _runtime, fileName: fileName);
}
