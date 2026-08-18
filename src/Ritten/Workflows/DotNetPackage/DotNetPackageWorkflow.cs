using Ritten.Engine;

namespace Ritten.Workflows.DotNetPackage;

/// <summary>
/// The jobs for building and maintaining NuGet packages.
/// </summary>
public class DotNetPackageWorkflow : IWorkflow
{
    /// <inheritdoc/>
    public string Name => "dotnet-package";

    /// <inheritdoc/>
    public string Label => "dotnet package";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } =
    [
        new StatusJob(),
        new BuildJob(),
        new CheckJob(),
        new DeployJob()
    ];
}
