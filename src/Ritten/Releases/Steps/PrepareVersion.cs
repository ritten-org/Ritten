using Microsoft.Extensions.Options;
using Ritten.Changelogs.Steps;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Reporting;

namespace Ritten.Releases.Steps;

/// <summary>
/// Writes the prepared version into whichever files declare it.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="options">The workflow's .NET options.</param>
/// <param name="dotnet">The dotnet client.</param>
[Step("prepare version", StepKind.Work)]
public class PrepareVersion(IWorkflowLog log, IOptions<DotNetOptions> options, IDotNet dotnet)
{
    /// <summary>
    /// Sets the version every shipped project evaluates to.
    /// </summary>
    /// <param name="project">The project being released (see <see cref="DotNet.Steps.ResolveRelease"/>).</param>
    /// <param name="release">The version being prepared (see <see cref="DecideVersion"/>).</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(Project project, PreparedRelease release, CancellationToken ct = default)
    {
        if (!release.Bumped)
        {
            log.Skipped($"The project already declares {release.Version}.");
            return StepResult.Successful;
        }

        var written = await dotnet.SetVersion(
            new SetVersionArgs { Projects = options.Value.Projects, Current = project.Version, Version = release.Version },
            ct);

        if (written.IsError)
        {
            return StepResult.Failed(written.Errors);
        }

        foreach (var file in written.Value)
        {
            log.Detail($"Set the version to {release.Version} in {file}.");
        }

        return StepResult.Successful;
    }
}
