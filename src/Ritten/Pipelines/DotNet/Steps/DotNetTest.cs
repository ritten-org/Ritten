using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Core;
using Ritten.DotNet;
using Ritten.Reporting;

namespace Ritten.Pipelines.DotNet.Steps;

/// <summary>
/// Runs the tests, reporting the aggregated counts on success and the individual failures otherwise.
/// </summary>
/// <param name="options">The pipeline's .NET options.</param>
/// <param name="pipeline">The pipeline's directory layout options.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="dotnet">The dotnet client.</param>
/// <param name="report">The build report.</param>
public class DotNetTest(
    IOptions<DotNetOptions> options,
    IOptions<PipelineOptions> pipeline,
    IFileSystem fileSystem,
    IDotNet dotnet,
    IBuildReport report
) : IPipelineStep
{
    private const int MaxFailures = 20;

    /// <inheritdoc />
    public string Name => "dotnet test";

    /// <inheritdoc />
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        var resultsDirectory = fileSystem.ProjectRoot
            .GetDirectory(pipeline.Value.TempDirectory)
            .GetDirectory("test-results");

        var result = await dotnet.Test(
            new TestArgs
            {
                Configuration = options.Value.Configuration,
                NoBuild = true,
                ResultsDirectory = resultsDirectory
            },
            cancellationToken);

        if (result.Succeeded)
        {
            if (result.Total > 0)
            {
                report.Section("Tests").Success(
                    result.Skipped > 0
                        ? $"**{result.Passed}** tests passed, {result.Skipped} skipped."
                        : $"All **{result.Passed}** tests passed.");
            }

            return StepResult.Successful;
        }

        if (result.Failures.Count == 0)
        {
            report.Section("Tests").Failure("`dotnet test` failed — check the build logs for details.");
            return StepResult.Failed("Tests failed. Re-run with --verbose to see the output.");
        }

        var summary = $"{result.Failed} {(result.Failed == 1 ? "test" : "tests")} failed ({result.Passed} passed, {result.Skipped} skipped)";
        report.Section("Tests")
            .Failure($"**{result.Failed}** {(result.Failed == 1 ? "test" : "tests")} failed ({result.Passed} passed, {result.Skipped} skipped).")
            .Details("Failed tests", DescribeFailures(result.Failures));

        return StepResult.Failed([
            new Error($"{summary}:"),
            .. result.Failures.Take(MaxFailures).Select(f => new Error(
                f.Message.Length > 0 ? $"{f.TestName}: {f.Message}" : f.TestName)),
            .. result.Failures.Count > MaxFailures
                ? new[] { new Error($"…and {result.Failures.Count - MaxFailures} more") }
                : []
        ]);
    }

    private static string DescribeFailures(IReadOnlyList<TestFailure> failures)
    {
        var described = failures
            .Take(MaxFailures)
            .Select(f => f.Message.Length > 0 ? $"**`{f.TestName}`**\n```\n{f.Message}\n```" : $"**`{f.TestName}`**")
            .ToList();

        if (failures.Count > MaxFailures)
        {
            described.Add($"…and {failures.Count - MaxFailures} more");
        }

        return string.Join("\n\n", described);
    }
}
