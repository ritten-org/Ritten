using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Core;
using Ritten.Pipelines;
using Ritten.Reporting;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Fails the pipeline when <c>dotnet format</c> would make changes, reporting the unformatted files.
/// </summary>
/// <param name="options">The pipeline's build options.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="dotnet">The dotnet client.</param>
/// <param name="report">The build report.</param>
[Step("dotnet format", StepKind.Validation)]
public class DotnetFormat(
    IOptions<PipelineOptions> options,
    IFileSystem fileSystem,
    IDotNet dotnet,
    IBuildReport report
)
{
    /// <summary>
    /// Checks the solution's formatting without changing anything.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        var reportDirectory = fileSystem.ProjectRoot
            .GetDirectory(options.Value.TempDirectory)
            .GetDirectory("format");

        var result = await dotnet.CheckFormat(new FormatArgs { ReportDirectory = reportDirectory, NoRestore = true }, cancellationToken);
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
