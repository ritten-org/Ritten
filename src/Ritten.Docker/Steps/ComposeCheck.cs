using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Docker.Steps;

/// <summary>
/// Proves compose can read the stack's file before anything is built from it.
/// </summary>
/// <param name="docker">The docker client.</param>
/// <param name="fileSystem">The workflow's file system.</param>
/// <param name="log">The run's log.</param>
[Step("compose check", StepKind.Check)]
public class ComposeCheck(IDocker docker, IFileSystem fileSystem, IWorkflowLog log)
{
    /// <summary>
    /// Validates the component's compose file.
    /// </summary>
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        if (await docker.ComposeValidate(fileSystem.ProjectRoot, ct: cancellationToken) is { } error)
        {
            return StepResult.Failed(error);
        }

        log.Status("The compose file is valid.");
        return StepResult.Successful;
    }
}
