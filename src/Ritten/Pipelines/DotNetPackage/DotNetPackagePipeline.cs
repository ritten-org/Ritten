using Ritten.Core;

namespace Ritten.Pipelines.DotNetPackage;

/// <summary>
/// The jobs for building and maintaining NuGet packages.
/// </summary>
public class DotNetPackagePipeline : IPipeline
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
