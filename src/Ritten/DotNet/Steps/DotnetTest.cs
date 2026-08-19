using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Engine;
using Ritten.Reporting;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Runs the tests, reporting the aggregated counts on success and the individual failures otherwise.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="options">The workflow's .NET options.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="dotnet">The dotnet client.</param>
/// <param name="report">The build report.</param>
[Step("dotnet test", StepKind.Work)]
public class DotnetTest(
    IWorkflowLog log,
    IOptions<DotNetOptions> options,
    IFileSystem fileSystem,
    IDotNet dotnet,
    IWorkflowReport report
)
{
    private const int MaxFailures = 20;

    /// <summary>
    /// Runs the solution's tests.
    /// </summary>
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        var resultsDirectory = fileSystem.Temp.GetDirectory("test-results");

        var result = await dotnet.Test(
            new TestArgs
            {
                Configuration = options.Value.Configuration,
                NoBuild = true,
                ResultsDirectory = resultsDirectory,
                CollectCoverage = true
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
                log.Detail(result.Skipped > 0
                    ? $"{result.Passed} tests passed, {result.Skipped} skipped."
                    : $"All {result.Passed} tests passed.");
            }
            else
            {
                log.Detail("No tests ran.");
            }

            return StepResult.Successful;
        }

        if (result.Failures.Count == 0)
        {
            var section = report.Section("Tests").Failure("`dotnet test` failed before reporting any results.");
            if (result.FailureOutput.Count == 0)
            {
                return StepResult.Failed("Tests failed. Re-run with --verbose to see the output.");
            }

            section.Details("Output", $"```\n{string.Join('\n', result.FailureOutput)}\n```");
            return StepResult.Failed([
                new Error("`dotnet test` failed before reporting any results:"),
                .. result.FailureOutput.Select(line => new Error(line))
            ]);
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
