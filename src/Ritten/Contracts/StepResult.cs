namespace Ritten.Contracts;

/// <summary>
/// Represents the result of a pipeline step execution.
/// </summary>
/// <param name="ExitCode">The exit code of the individual step.</param>
/// <param name="Continue">True if the pipeline should continue execution, otherwise false.</param>
public record StepResult(int ExitCode, bool Continue)
{
    /// <summary>
    /// Represents a successful pipeline step execution with no exceptions and continuation.
    /// </summary>
    public static readonly StepResult Successful = new(PipelineExitCodes.Success, true);

    /// <summary>
    /// Indicates that cancellation was requested, and the pipeline should stop execution.
    /// </summary>
    public static readonly StepResult StoppedAfterCancel = new(PipelineExitCodes.StoppedAfterCancel, false);

    /// <summary>
    /// Indicates that the pipeline step resulted in an error, and the pipeline should stop execution.
    /// </summary>
    public static readonly StepResult StoppedOnError = new(PipelineExitCodes.StoppedOnError, false);
}
