using Ritten.Engine;
using Ritten.Engine.Runtimes;
using Ritten.Reporting;

namespace Ritten.Tests.Support;

/// <summary>
/// The runtime a test application runs in: detected from a marker no real environment sets, and printing to a console
/// the test can read.
/// </summary>
internal sealed class TestRuntime : Runtime
{
    public const string Marker = "RITTEN_TEST_RUNTIME";

    /// <summary>
    /// An environment holding the marker and nothing else.
    /// </summary>
    public static Func<string, string?> Environment { get; } = name => name == Marker ? "1" : null;

    public RecordingConsole Console { get; } = new();

    public override string Name => "test";

    public override IReadOnlyCollection<string> Markers { get; } = [Marker];

    public override IReadOnlyCollection<string> Claims { get; } = [Marker];

    public override void Configure(IWorkflowBuilder builder, Func<string, string?> environment)
    {
    }

    public override IWorkflowConsole CreateConsole(WorkflowLogLevel level) => Console;
}
