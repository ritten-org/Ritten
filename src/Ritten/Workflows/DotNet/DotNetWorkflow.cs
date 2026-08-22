using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Engine.Workflows;

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
        new InitJob(),
        new BuildJob(),
        new CheckJob()
    ];
}
