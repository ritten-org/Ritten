using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.Reporting;

namespace Ritten.Init.Steps;

/// <summary>
/// Makes sure the repository pins the tool version that set it up.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="dotnet">The .NET client.</param>
/// <param name="git">The git client, for the root the manifest belongs at.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="tool">The tool being pinned.</param>
[Step("ensure tool manifest", StepKind.Work)]
public class EnsureToolManifest(IWorkflowLog log, IDotNet dotnet, IGit git, IFileSystem fileSystem, ToolPin tool)
{
    /// <summary>
    /// Pins the tool in whichever manifest governs the repository.
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        // One manifest at the repository's root serves every project in it.
        var root = await git.RepositoryRoot(ct) ?? fileSystem.ProjectRoot;
        var scope = ToolScope.Local(root);
        var pinned = await dotnet.InstalledToolVersion(tool.PackageId, scope, ct);
        if (pinned == tool.Version)
        {
            log.Skipped($"{tool.PackageId} {tool.Version} is already pinned.");
            return StepResult.Successful;
        }

        if (DotNetProjects.ToolManifest(root) is null)
        {
            await dotnet.CreateToolManifest(root, ct);
            log.Detail($"{DotNetProjects.ToolManifestDirectory}: a tool manifest for the repository.");
        }

        var args = new ToolInstallArgs { PackageId = tool.PackageId, Scope = scope, Version = tool.Version };
        if (pinned is null)
        {
            await dotnet.ToolInstall(args, ct);
            log.Detail($"The tool manifest: {tool.PackageId} {tool.Version}.");
            return StepResult.Successful;
        }

        await dotnet.ToolUpdate(args, ct);
        log.Detail($"The tool manifest: {tool.PackageId} {tool.Version}, up from {pinned}.");

        return StepResult.Successful;
    }
}
