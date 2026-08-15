using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Pipelines;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Reads and combines the cobertura reports the test run produced.
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="pipeline">The pipeline's directory layout options.</param>
/// <param name="fileSystem">The file system.</param>
[Step("read coverage", StepKind.Work)]
public class ReadCoverage(IPipelineLog log, IOptions<PipelineOptions> pipeline, IFileSystem fileSystem)
{
    /// <summary>
    /// Reads the coverage reports from the test results directory.
    /// </summary>
    public StepResult<Coverage> Run()
    {
        var results = fileSystem.ProjectRoot
            .GetDirectory(pipeline.Value.TempDirectory)
            .GetDirectory("test-results");

        var files = results.GetFiles("**/coverage.cobertura.xml").ToList();
        if (files.Count == 0)
        {
            return StepResult.Failed("No coverage reports were produced. Add the coverlet.collector package to your test projects.");
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
