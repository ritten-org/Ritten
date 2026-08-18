using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.CodeCoverage.Steps;

/// <summary>
/// Reads and combines the cobertura reports the test run produced.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="fileSystem">The file system.</param>
[Step("read coverage", StepKind.Work)]
public class ReadCoverage(IWorkflowLog log, IFileSystem fileSystem)
{
    /// <summary>
    /// Reads the coverage reports from the test results directory.
    /// </summary>
    public StepResult<Coverage> Run()
    {
        var results = fileSystem.Temp.GetDirectory("test-results");

        var files = results.GetFiles("**/coverage.cobertura.xml").ToList();
        if (files.Count == 0)
        {
            return StepResult.Failed("No coverage reports were produced. Add the Microsoft.Testing.Extensions.CodeCoverage package to your test projects.");
        }

        var coverage = files
            .Select(file =>
            {
                using var stream = file.OpenRead();
                return Coverage.Parse(stream);
            })
            .Aggregate((left, right) => left + right);

        log.Detail($"Line coverage {coverage.LineRate:0.0}%, branch coverage {coverage.BranchRate:0.0}%"
            + (files.Count > 1 ? $" across {files.Count} reports." : "."));
        return coverage;
    }
}
