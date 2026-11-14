using Ritten.Contracts;

namespace Ritten.Npm.Steps;

/// <summary>
/// Installs the project's dependencies, exactly as its lock file says.
/// </summary>
/// <param name="npm">The npm client.</param>
[Step("npm install", StepKind.Work)]
public class NpmInstall(INpm npm)
{
    /// <summary>
    /// Installs the dependencies.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(NpmProject project, CancellationToken cancellationToken = default)
    {
        await npm.Install(project, cancellationToken);
        return StepResult.Successful;
    }
}
