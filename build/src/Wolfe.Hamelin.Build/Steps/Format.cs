using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.DotNet;
using Wolfe.Hamelin.Reporting;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Validate Code Formatting")]
public class Format(
    IOptions<BuildOptions> options,
    IPipelineContext context,
    IDotNet dotnet,
    IBuildReport report
) : IPipelineStep
{
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
