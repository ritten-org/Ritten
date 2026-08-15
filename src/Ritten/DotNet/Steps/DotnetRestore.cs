using Ritten.Contracts;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Runs <c>dotnet restore</c>.
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="dotnet">The dotnet client.</param>
[Step("dotnet restore", StepKind.Work)]
public class DotnetRestore(IPipelineLog log, IDotNet dotnet)
{
    /// <summary>
    /// Restores the solution's packages.
    /// </summary>
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        var result = await dotnet.Restore(new RestoreArgs(), cancellationToken);
        log.Detail(result.RestoredProjects.Count == 0
            ? "All projects were already up to date."
            : $"Restored {string.Join(", ", result.RestoredProjects)}.");
        return StepResult.Successful;
    }
}
