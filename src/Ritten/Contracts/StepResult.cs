namespace Ritten.Contracts;

/// <summary>
/// Represents the result of a pipeline step execution.
/// </summary>
/// <param name="ExitCode">The exit code of the individual step.</param>
/// <param name="Continue">True if the pipeline should continue execution, otherwise false.</param>
/// <param name="Message">An optional human-readable message describing the outcome.</param>
public record StepResult(int ExitCode, bool Continue, string Message)
{
    /// <summary>
    /// Represents a successful pipeline step execution with no exceptions and continuation.
    /// </summary>
    public static readonly StepResult Successful = new(PipelineExitCodes.Success, true, "Success");

    /// <summary>
    /// Indicates that cancellation was requested, and the pipeline should stop execution.
    /// </summary>
    public static readonly StepResult StoppedAfterCancel = new(PipelineExitCodes.Cancelled, false, "Stopped after cancel");

    /// <summary>
    /// Indicates that the step failed with a human-readable error message.
    /// </summary>
    public static StepResult Failed(string message) => new(PipelineExitCodes.Failed, false, message);

    /// <summary>
    /// Gets whether this result represents a failure.
    /// </summary>
    public bool IsFailure => ExitCode != PipelineExitCodes.Success;
}
