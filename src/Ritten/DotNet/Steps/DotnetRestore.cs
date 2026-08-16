using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Runs <c>dotnet restore</c>, reporting the restore diagnostics when it fails.
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="dotnet">The dotnet client.</param>
/// <param name="report">The build report.</param>
[Step("dotnet restore", StepKind.Work)]
public class DotnetRestore(IPipelineLog log, IDotNet dotnet, IBuildReport report)
{
    /// <summary>
    /// Restores the solution's packages.
    /// </summary>
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        var result = await dotnet.Restore(new RestoreArgs(), cancellationToken);
        if (result.Succeeded)
        {
            log.Detail(result.RestoredProjects.Count == 0
                ? "All projects were already up to date."
                : $"Restored {string.Join(", ", result.RestoredProjects)}.");
            return StepResult.Successful;
        }

        var section = report.Section("Restore").Failure("The solution's packages failed to restore.");
        if (result.Diagnostics.Count == 0)
        {
            return StepResult.Failed("The solution's packages failed to restore. Re-run with --verbose to see the restore output.");
        }

        return section.FailWithDiagnostics("Restore output", result.Diagnostics);
    }
}
