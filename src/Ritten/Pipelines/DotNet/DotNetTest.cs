using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Options;
using Ritten.DotNet;
using Ritten.Reporting;

namespace Ritten.Pipelines.DotNet;

/// <summary>
/// Runs the tests, reporting the aggregated counts on success and the individual failures otherwise.
/// </summary>
/// <param name="options">The pipeline's .NET options.</param>
/// <param name="pipeline">The pipeline's directory layout options.</param>
/// <param name="context">The pipeline context.</param>
/// <param name="dotnet">The dotnet client.</param>
/// <param name="report">The build report.</param>
[DisplayName("Run .NET Tests")]
public class DotNetTest(
    IOptions<DotNetOptions> options,
    IOptions<PipelineOptions> pipeline,
    IPipelineContext context,
    IDotNet dotnet,
    IBuildReport report
) : IPipelineStep
{
    private const int MaxFailures = 20;

    /// <inheritdoc />
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var resultsDirectory = context.FileSystem.CurrentDirectory
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

            return;
        }

        if (result.Failures.Count == 0)
        {
            report.Section("Tests").Failure("`dotnet test` failed — check the build logs for details.");
        }
        else
        {
            report.Section("Tests")
                .Failure($"**{result.Failed}** {(result.Failed == 1 ? "test" : "tests")} failed ({result.Passed} passed, {result.Skipped} skipped).")
                .Details("Failed tests", DescribeFailures(result.Failures));
        }

        throw new Exception("Tests failed.");
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
