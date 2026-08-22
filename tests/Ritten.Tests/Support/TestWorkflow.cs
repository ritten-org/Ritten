using Ritten.Contracts.FileSystem;
using Ritten.Engine.Workflows;

namespace Ritten.Tests.Support;

/// <summary>
/// A workflow declared inline: the jobs a test hands it, nothing more.
/// </summary>
internal sealed class TestWorkflow(
    string name = "test",
    IReadOnlyList<IJob>? jobs = null,
    string? label = null,
    string? recognises = null
) : IWorkflow
{
    public string Name => name;

    public string Label => label ?? "Test";

    public IReadOnlyList<IJob> Jobs { get; } = jobs ?? [];

    public Task<string?> IsCompatible(IDirectory repository, CancellationToken cancellationToken = default) =>
        Task.FromResult(recognises);
}
