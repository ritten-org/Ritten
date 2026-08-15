using System.Diagnostics.CodeAnalysis;
using Ritten.Core;

namespace Ritten.Contracts;

/// <summary>
/// Represents the result of a pipeline step execution.
/// </summary>
/// <param name="ExitCode">The exit code of the individual step.</param>
/// <param name="Continue">True if the pipeline should continue execution, otherwise false.</param>
/// <param name="Errors">Any errors associated with the result.</param>
public record StepResult(int ExitCode, bool Continue, IReadOnlyCollection<Error>? Errors)
{
    /// <summary>
    /// Represents a successful pipeline step execution with no exceptions and continuation.
    /// </summary>
    public static readonly StepResult Successful = new(PipelineExitCodes.Success, true, null);

    /// <summary>
    /// Indicates that cancellation was requested, and the pipeline should stop execution.
    /// </summary>
    public static readonly StepResult StoppedAfterCancel = new(PipelineExitCodes.Cancelled, false, [new Error("Stopped after cancel.")]);

    /// <summary>
    /// Indicates that the step found no work left for the job: the pipeline stops here, successfully.
    /// </summary>
    public static readonly StepResult NothingToDo = new(PipelineExitCodes.Success, false, null);

    /// <summary>
    /// Indicates that the step failed with an error.
    /// </summary>
    public static StepResult Failed(Error error) => new(PipelineExitCodes.Failed, false, [error]);

    /// <summary>
    /// Indicates that the step failed with multiple errors.
    /// </summary>
    public static StepResult Failed(IEnumerable<Error> errors) => new(PipelineExitCodes.Failed, false, [.. errors]);

    /// <summary>
    /// Gets whether this result represents a failure.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Errors))]
    public bool IsFailure => ExitCode != PipelineExitCodes.Success;
}
