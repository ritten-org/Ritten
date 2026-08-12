using System.ComponentModel;
using System.Text.Json;
using Hamelin;
using Hamelin.FileSystem;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Commands;
using Wolfe.Hamelin.Reporting;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Validate Code Formatting")]
public class Format(
    IOptions<BuildOptions> options,
    IPipelineContext context,
    ICommandRunner commands,
    IBuildReport report
) : IPipelineStep
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task Run(CancellationToken cancellationToken = default)
    {
        var reportDirectory = context.FileSystem.CurrentDirectory
            .GetDirectory(options.Value.TempDirectory)
            .GetDirectory("format");
        reportDirectory.Create();

        var dotnetFormat = Command.Create("dotnet").WithArguments("format", "--verify-no-changes", "--report", reportDirectory.AbsolutePath);
        var result = await commands.Run(dotnetFormat, cancellationToken);
        if (result.IsSuccess)
        {
            return;
        }

        var files = await ReadUnformattedFiles(reportDirectory.GetFile("format-report.json"), cancellationToken);
        if (files.Count > 0)
        {
            report.Section("Formatting").Failure(
                $"{files.Count} {(files.Count == 1 ? "file isn't" : "files aren't")} formatted — run `dotnet format` and commit the result:\n" +
                string.Join('\n', files.Select(f => $"- `{f}`")));
        }
        else
        {
            report.Section("Formatting").Failure("`dotnet format --verify-no-changes` failed — check the build logs for details.");
        }

        throw new Exception("Code formatting check failed.");
    }

    private async Task<IReadOnlyList<string>> ReadUnformattedFiles(IFile reportFile, CancellationToken cancellationToken)
    {
        if (!reportFile.Exists)
        {
            return [];
        }

        await using var stream = reportFile.OpenRead();
        var documents = await JsonSerializer.DeserializeAsync<List<FormatReportDocument>>(stream, JsonOptions, cancellationToken) ?? [];
        return documents
            .Where(d => d.FilePath != null)
            .Select(d => Path.GetRelativePath(context.CurrentDirectory, d.FilePath!))
            .Distinct()
            .Order()
            .ToList();
    }

    private sealed record FormatReportDocument(string? FilePath);
}
