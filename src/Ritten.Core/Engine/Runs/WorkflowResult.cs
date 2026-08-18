namespace Ritten.Engine.Runs;

/// <summary>
/// Represents the result of a workflow execution, including the exit code and the outcome of each step.
/// </summary>
public class WorkflowResult(int exitCode, IEnumerable<StepOutcome> steps)
{
    /// <summary>
    /// Gets the exit code of the overall workflow execution.
    /// </summary>
    public int ExitCode { get; } = exitCode;

    /// <summary>
    /// Gets the outcome of each step in the workflow execution.
    /// </summary>
    public IReadOnlyCollection<StepOutcome> Steps { get; init; } = steps.ToList().AsReadOnly();

    /// <summary>
    /// Gets whether the workflow was successful.
    /// </summary>
    public bool IsSuccess => ExitCode == 0;

    /// <summary>
    /// Gets the first failure of the run, when there was one.
    /// </summary>
    public StepOutcome? FailedStep => Steps.FirstOrDefault(s => s.Result.IsFailure);
}
