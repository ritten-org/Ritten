using Ritten.Engine;
using Ritten.Engine.Runtimes;
using Ritten.Reporting;

namespace Ritten.Tests.Engine.Runtimes;

public class DetectRuntimeResultTests
{
    [Fact]
    public void Debug_IsOffWhenTheRuntimeDoesNotAskForIt()
    {
        var selection = Detect(new DebuggableRuntime(), ("STUB_CI", "true"));

        selection.Debug.ShouldBeFalse();
    }

    [Fact]
    public void Debug_IsReadFromTheRuntimesOwnClaims()
    {
        // The debug marker is claimed, so the filtered environment hides it — but a claim is
        // consumption, not discarding: its owner still reads it.
        var selection = Detect(new DebuggableRuntime(), ("STUB_CI", "true"), ("STUB_DEBUG", "on"));

        selection.Debug.ShouldBeTrue();
        selection.Environment("STUB_DEBUG").ShouldBeNull();
    }

    [Fact]
    public void Debug_StaysOffForTheLocalFallback()
    {
        // A debug marker without its runtime is just a stray variable; nothing present owns it,
        // so nobody honours it.
        var selection = new RuntimeRegistry().Detect(Env(("STUB_DEBUG", "on"))).Value.ShouldNotBeNull();

        selection.Debug.ShouldBeFalse();
    }

    [Fact]
    public void CreateConsole_AsksTheRuntimeForTheRequestedLevel()
    {
        var runtime = new DebuggableRuntime();
        var selection = Detect(runtime, ("STUB_CI", "true"));

        selection.CreateConsole(WorkflowLogLevel.Detail);

        runtime.ConsoleLevel.ShouldBe(WorkflowLogLevel.Detail);
    }

    [Fact]
    public void CreateConsole_FloorsTheLevelAtVerboseOnADebugRequest()
    {
        // Re-running with debug logging is an in-the-moment ask, so it outranks a --quiet that's
        // been sitting in a workflow file since whenever.
        var runtime = new DebuggableRuntime();
        var selection = Detect(runtime, ("STUB_CI", "true"), ("STUB_DEBUG", "on"));

        selection.CreateConsole(WorkflowLogLevel.Warning);

        runtime.ConsoleLevel.ShouldBe(WorkflowLogLevel.Verbose);
    }

    [Fact]
    public void CreateConsole_RendersWithTheEngineRendererByDefault()
    {
        var selection = new RuntimeRegistry().Detect(Env()).Value.ShouldNotBeNull();

        selection.CreateConsole(WorkflowLogLevel.Detail).ShouldBeOfType<SpectreWorkflowConsole>();
    }

    private static DetectRuntimeResult Detect(Runtime runtime, params (string Name, string Value)[] variables) =>
        new RuntimeRegistry().Add(runtime).Detect(Env(variables)).Value.ShouldNotBeNull();

    private static Func<string, string?> Env(params (string Name, string Value)[] variables) =>
        variables.ToDictionary(v => v.Name, v => v.Value).GetValueOrDefault;

    private sealed class DebuggableRuntime : Runtime
    {
        public WorkflowLogLevel? ConsoleLevel { get; private set; }

        public override string Name => "debuggable";

        public override IReadOnlyCollection<string> Markers { get; } = ["STUB_CI"];

        public override IReadOnlyCollection<string> Claims { get; } = ["STUB_CI", "STUB_DEBUG"];

        public override void Configure(IWorkflowBuilder builder, Func<string, string?> environment)
        {
        }

        public override bool IsDebug(Func<string, string?> environment) => environment("STUB_DEBUG") == "on";

        public override IWorkflowConsole CreateConsole(WorkflowLogLevel level)
        {
            ConsoleLevel = level;
            return Substitute.For<IWorkflowConsole>();
        }
    }
}
