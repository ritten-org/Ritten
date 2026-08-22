using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
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
        new InitJob(),
        new StatusJob(),
        new BuildJob(),
        new InstallJob(),
        new PrepareJob(),
        new CheckJob(),
        new DeployJob()
    ];

    /// <inheritdoc />
    public async Task<string?> IsCompatible(IDirectory repository, CancellationToken cancellationToken = default) =>
        await DotNetProjects.FileContainingMsBuildElement(repository, "<PackAsTool>true</PackAsTool>", cancellationToken) is { } project
            ? $"{repository.RelativePath(project)} packs as a tool"
            : null;
}
