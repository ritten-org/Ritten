using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Engine.Workflows;

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
        new InitJob(),
        new StatusJob(),
        new BuildJob(),
        new PrepareJob(),
        new CheckJob(),
        new DeployJob()
    ];

    /// <inheritdoc />
    public async Task<string?> IsCompatible(IDirectory repository, CancellationToken cancellationToken = default)
    {
        foreach (var element in (string[])["<PackageId>", "<IsPackable>true</IsPackable>"])
        {
            if (await DotNetProjects.FileContainingMsBuildElement(repository, element, cancellationToken) is { } project)
            {
                return $"{repository.RelativePath(project)} packs as a package";
            }
        }

        return null;
    }
}
