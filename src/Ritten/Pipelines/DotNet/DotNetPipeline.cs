using Ritten.Core;

namespace Ritten.Pipelines.DotNet;

/// <summary>
/// The jobs for a project that ships nothing.
/// </summary>
public class DotNetPipeline : IPipeline
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
