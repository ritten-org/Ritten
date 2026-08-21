using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Formats the solution, fixing what <see cref="DotnetFormatCheck"/> would otherwise only report.
/// </summary>
/// <param name="job">The job being run.</param>
/// <param name="log">The workflow log.</param>
/// <param name="dotnet">The dotnet client.</param>
[Step("dotnet format", StepKind.Work)]
public class DotnetFormat(WorkflowJob job, IWorkflowLog log, IDotNet dotnet)
{
    /// <summary>
    /// Formats the solution.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        var result = await dotnet.Format(new FormatArgs(), cancellationToken);
        if (!result.Succeeded)
        {
            log.Warning("Could not format the solution. Fix the build and run `dotnet format` again.");
            return StepResult.Successful;
        }

        var count = result.UnformattedFiles.Count;
        var files = count == 1 ? "file" : "files";
        if (count == 0)
        {
            log.Detail("Everything is already formatted.");
            return StepResult.Successful;
        }

        // (The "dry run" version of format just does a check.)
        if (job.DryRun)
        {
            log.Skipped($"Would format {count} {files}.");
        }
        else
        {
            log.Detail($"Formatted {count} {files}.");
        }

        foreach (var file in result.UnformattedFiles)
        {
            log.Verbose(file);
        }

        return StepResult.Successful;
    }
}
