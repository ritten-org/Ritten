using Ritten.Engine;
using Ritten.Engine.Runtimes;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Engine.Runs;

sealed class StubRuntime : Runtime
{
    public string? SeenSecret { get; private set; }

    public RecordingConsole Console { get; } = new();

    public override string Name => "stub";

    public override IReadOnlyCollection<string> Markers { get; } = ["STUB_CI"];

    public override IReadOnlyCollection<string> Claims { get; } = ["STUB_CI", "STUB_SECRET"];

    public override void Configure(IWorkflowBuilder builder, Func<string, string?> environment) =>
        SeenSecret = environment("STUB_SECRET");

    public override IWorkflowConsole CreateConsole(WorkflowLogLevel level) => Console;
}
