using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Installs every packed tool globally, so the working tree's build runs from anywhere.
/// </summary>
/// <param name="job">The job being run.</param>
/// <param name="force">Whether to replace an install that already carries this version.</param>
/// <param name="log">The workflow log.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="dotnet">The dotnet client.</param>
[Step("dotnet tool install", StepKind.Work)]
public class DotnetToolInstall(WorkflowJob job, ForceReinstall force, IWorkflowLog log, IFileSystem fileSystem, IDotNet dotnet)
{
    /// <summary>
    /// Installs each tool the repository ships.
    /// </summary>
    /// <param name="packages">The packages the repository ships (see <see cref="ReadProjects"/>).</param>
    /// <param name="packed">The packages just packed (see <see cref="DotnetPack"/>).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(PackageSet packages, PackResult packed, CancellationToken cancellationToken = default)
    {
        var tools = packages.Packages.Where(p => p.IsTool).ToList();
        if (tools.Count == 0)
        {
            return StepResult.Failed("None of the shipped projects packs as a tool. Set <PackAsTool> in the tool's project file.");
        }

        var installed = 0;
        foreach (var tool in tools)
        {
            var package = $"{tool.Name}.{tool.Version}.nupkg";
            if (!packed.Packages.Any(f => string.Equals(f.Name, package, StringComparison.OrdinalIgnoreCase)))
            {
                return StepResult.Failed($"{tool.Name} {tool.Version} was not packed; expected {package} in the artifacts.");
            }

            var current = await dotnet.InstalledToolVersion(tool.Name, cancellationToken);
            if (current == tool.Version && !force.Requested)
            {
                log.Skipped($"{tool.Name} {tool.Version} is already installed; pass --{ToolArguments.Reinstall.Name} to reinstall this build.");
                continue;
            }

            if (current is not null)
            {
                // `dotnet tool install` refuses while any version is installed, so replacing —
                // same version or not — starts by removing the old install.
                await dotnet.ToolUninstall(tool.Name, cancellationToken);
            }

            await dotnet.ToolInstall(
                new ToolInstallArgs { PackageId = tool.Name, Version = tool.Version, Source = fileSystem.Artifacts },
                cancellationToken);
            installed++;

            // In a rehearsal the decorated client already narrates what would happen.
            if (!job.DryRun)
            {
                var command = tool.ToolCommand is null ? "" : $" Run '{tool.ToolCommand}' from anywhere.";
                log.Detail(current is null || current == tool.Version
                    ? $"Installed {tool.Name} {tool.Version} globally.{command}"
                    : $"Replaced {tool.Name} {current} with {tool.Version}.{command}");
            }
        }

        // Every tool already carries this exact build's version: stop here, successfully.
        return installed == 0 ? StepResult.NothingToDo : StepResult.Successful;
    }
}
