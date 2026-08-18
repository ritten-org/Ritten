using Ritten.Engine;

namespace Ritten.Workflows.DotNet;

/// <summary>
/// The jobs for a project that ships nothing.
/// </summary>
public class DotNetWorkflow : IWorkflow
{
    /// <inheritdoc/>
    public string Name => "dotnet";

    /// <inheritdoc/>
    public string Label => "dotnet";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } =
    [
        new BuildJob(),
        new CheckJob()
    ];
}
