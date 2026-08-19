using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Git;
using Ritten.Reporting;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Resolves the release's identity from the shipped projects.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="options">The workflow's .NET options.</param>
/// <param name="git">The git client.</param>
[Step("resolve release", StepKind.Work)]
public class ResolveRelease(IWorkflowLog log, IOptions<DotNetOptions> options, IGit git)
{
    /// <summary>
    /// Derives the release's identity from the already-read projects.
    /// </summary>
    /// <param name="packages">The packages the repository ships (see <see cref="ReadProjects"/>).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult<Project>> Run(PackageSet packages, CancellationToken cancellationToken = default)
    {
        if (packages.Packages.Count == 0)
        {
            return StepResult.Failed("There are no projects configured to ship.");
        }

        var release = packages.Packages[0];

        // Resolved once, here, so no consumer coalesces sources again: the explicit setting
        // wins, then the project file's RepositoryUrl, then the origin remote.
        var repository = options.Value.Repository
            ?? release.Repository
            ?? RepositoryUrls.ToWebUrl(await git.GetRemoteUrl("origin", cancellationToken));

        log.Detail($"Releasing as {release.Name} (v{release.Version}).");
        log.Verbose($"Repository: {repository ?? "unknown"}.");
        return release with { Repository = repository };
    }
}
