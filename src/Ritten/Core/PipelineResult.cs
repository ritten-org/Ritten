using Ritten.Contracts;

namespace Ritten.Core;

/// <summary>
/// Represents the result of a pipeline execution, including the exit code and results of each step.
/// </summary>
public class PipelineResult(int exitCode, IEnumerable<StepResult> steps)
{
    /// <summary>
    /// Gets the exit code of the overall pipeline execution.
    /// </summary>
    public int ExitCode { get; } = exitCode;

    /// <summary>
    /// Gets the results of each step in the pipeline execution.
    /// </summary>
    public IReadOnlyCollection<StepResult> Steps { get; init; } = steps.ToList().AsReadOnly();
}
