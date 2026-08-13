using System.ComponentModel;
using Ritten.Contracts;
using Microsoft.Extensions.Options;
using Ritten.DotNet;
using Ritten.Reporting;

namespace Ritten.Pipelines.DotNet;

/// <summary>
/// Fails the pipeline when <c>dotnet format</c> would make changes, reporting the unformatted files.
/// </summary>
/// <param name="options">The pipeline's build options.</param>
/// <param name="context">The pipeline context.</param>
/// <param name="dotnet">The dotnet client.</param>
/// <param name="report">The build report.</param>
[DisplayName("Check .NET Formatting")]
public class DotNetFormatCheck(
    IOptions<PipelineOptions> options,
    IPipelineContext context,
    IDotNet dotnet,
    IBuildReport report
) : IPipelineStep
{
    /// <inheritdoc />
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var reportDirectory = context.FileSystem.CurrentDirectory
            .GetDirectory(options.Value.TempDirectory)
            .GetDirectory("format");

        var result = await dotnet.CheckFormat(new FormatArgs { ReportDirectory = reportDirectory }, cancellationToken);
        if (result.Succeeded)
        {
            return;
        }

        if (result.UnformattedFiles.Count > 0)
        {
            report.Section("Formatting").Failure(
                $"{result.UnformattedFiles.Count} {(result.UnformattedFiles.Count == 1 ? "file isn't" : "files aren't")} formatted — run `dotnet format` and commit the result:\n" +
                string.Join('\n', result.UnformattedFiles.Select(f => $"- `{f}`")));
        }
        else
        {
            report.Section("Formatting").Failure("`dotnet format --verify-no-changes` failed — check the build logs for details.");
        }

        throw new Exception("Code formatting check failed.");
    }
}
