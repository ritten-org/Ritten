using Ritten.Engine;
using Ritten.Engine.Workflows;

namespace Ritten.Workflows.DotNetTool;

/// <summary>
/// The jobs for building and maintaining .NET tools.
/// </summary>
public class DotNetToolWorkflow : IWorkflow
{
    /// <inheritdoc/>
    public string Name => "dotnet-tool";

    /// <inheritdoc/>
    public string Label => "dotnet tool";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } =
    [
        new StatusJob(),
        new BuildJob(),
        new CheckJob(),
        new DeployJob()
    ];
}
