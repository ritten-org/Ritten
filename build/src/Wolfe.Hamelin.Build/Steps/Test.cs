using System.ComponentModel;
using System.Xml.Linq;
using Hamelin;
using Hamelin.FileSystem;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Commands;
using Wolfe.Hamelin.Reporting;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Run Tests")]
public class Test(
    IOptions<BuildOptions> options,
    IPipelineContext context,
    ICommandRunner commands,
    IBuildReport report
) : IPipelineStep
{
    private const int MaxFailures = 20;

    public async Task Run(CancellationToken cancellationToken = default)
    {
        var resultsDirectory = context.FileSystem.CurrentDirectory
            .GetDirectory(options.Value.TempDirectory)
            .GetDirectory("test-results");
        resultsDirectory.Create();

        var dotnetTest = Command
            .Create("dotnet")
            .WithArguments("test", "--no-build")
            .AndArguments("--configuration", options.Value.Configuration)
            .AndArguments("--logger", "trx")
            .AndArguments("--results-directory", resultsDirectory.AbsolutePath);
        var result = await commands.Run(dotnetTest, cancellationToken);

        var runs = resultsDirectory.GetFiles("*.trx").Select(ParseTrx).ToList();
        var passed = runs.Sum(r => r.Passed);
        var failed = runs.Sum(r => r.Failed);
        var skipped = runs.Sum(r => r.Skipped);
        var failures = runs.SelectMany(r => r.Failures).ToList();

        if (result.IsSuccess)
        {
            if (passed + failed + skipped > 0)
            {
                report.Section("Tests").Success(
                    skipped > 0
                        ? $"**{passed}** tests passed, {skipped} skipped."
                        : $"All **{passed}** tests passed.");
            }

            return;
        }

        if (failures.Count == 0)
        {
            report.Section("Tests").Failure("`dotnet test` failed — check the build logs for details.");
        }
        else
        {
            report.Section("Tests")
                .Failure($"**{failed}** {(failed == 1 ? "test" : "tests")} failed ({passed} passed, {skipped} skipped).")
                .Details("Failed tests", DescribeFailures(failures));
        }

        throw new Exception("Tests failed.");
    }

    private static string DescribeFailures(IReadOnlyList<TestFailure> failures)
    {
        var described = failures
            .Take(MaxFailures)
            .Select(f => f.Message.Length > 0 ? $"**`{f.Name}`**\n```\n{f.Message}\n```" : $"**`{f.Name}`**")
            .ToList();

        if (failures.Count > MaxFailures)
        {
            described.Add($"…and {failures.Count - MaxFailures} more");
        }

        return string.Join("\n\n", described);
    }

    private static TestRunSummary ParseTrx(IFile file)
    {
        using var stream = file.OpenRead();
        var document = XDocument.Load(stream);
        var counters = document.Descendants().FirstOrDefault(e => e.Name.LocalName == "Counters");
        int Count(string attribute) => (int?)counters?.Attribute(attribute) ?? 0;

        var failures = document.Descendants()
            .Where(e => e.Name.LocalName == "UnitTestResult" && (string?)e.Attribute("outcome") == "Failed")
            .Select(e => new TestFailure(
                (string?)e.Attribute("testName") ?? "(unknown test)",
                e.Descendants().FirstOrDefault(m => m.Name.LocalName == "Message")?.Value.Trim() ?? ""))
            .ToList();

        return new TestRunSummary(Count("passed"), Count("failed"), Count("notExecuted"), failures);
    }

    private sealed record TestRunSummary(int Passed, int Failed, int Skipped, IReadOnlyList<TestFailure> Failures);

    private sealed record TestFailure(string Name, string Message);
}
