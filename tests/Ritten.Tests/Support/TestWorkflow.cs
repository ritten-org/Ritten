using Ritten.Core;

namespace Ritten.Tests.Support;

/// <summary>
/// A workflow declared inline: the jobs a test hands it, nothing more.
/// </summary>
internal sealed class TestWorkflow(string name = "test", IReadOnlyList<IJob>? jobs = null) : IWorkflow
{
    public string Name => name;

    public string Label => "Test";

    public IReadOnlyList<IJob> Jobs { get; } = jobs ?? [];
}
