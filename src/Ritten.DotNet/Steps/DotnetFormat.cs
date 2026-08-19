using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Reporting;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Fails the workflow when <c>dotnet format whitespace</c> would make changes.
/// </summary>
/// <param name="dotnet">The dotnet client.</param>
/// <param name="report">The build report.</param>
[Step("check formatting", StepKind.Check)]
public class DotnetFormat(IDotNet dotnet, IWorkflowReport report)
{
    /// <summary>
    /// Checks the solution's formatting without changing anything.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        var result = await dotnet.CheckFormat(new FormatArgs { NoRestore = true }, cancellationToken);
        if (result.Succeeded)
        {
            return StepResult.Successful;
        }

        if (result.UnformattedFiles.Count == 0)
        {
            report.Section("Formatting").Failure("`dotnet format --verify-no-changes` failed — check the build logs for details.");
            return StepResult.Failed("Formatting check failed. Re-run with --verbose to see the output.");
        }

        var summary = $"{result.UnformattedFiles.Count} {(result.UnformattedFiles.Count == 1 ? "file isn't" : "files aren't")} formatted — run `dotnet format` and commit the result";
        report.Section("Formatting").Failure(
            $"{summary}:\n" + string.Join('\n', result.UnformattedFiles.Select(f => $"- `{f}`")));

        return StepResult.Failed([
            new Error($"{summary}:"),
            .. result.UnformattedFiles.Select(f => new Error(f))
        ]);
    }
}
