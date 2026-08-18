using System.Diagnostics.CodeAnalysis;
using Ritten.Engine;

namespace Ritten.Contracts;

/// <summary>
/// Represents the result of a workflow step execution.
/// </summary>
/// <param name="ExitCode">The exit code of the individual step.</param>
/// <param name="Continue">True if the workflow should continue execution, otherwise false.</param>
/// <param name="Errors">Any errors associated with the result.</param>
public record StepResult(int ExitCode, bool Continue, IReadOnlyCollection<Error>? Errors)
{
    /// <summary>
    /// Represents a successful workflow step execution with no exceptions and continuation.
    /// </summary>
    public static readonly StepResult Successful = new(WorkflowExitCodes.Success, true, null);

    /// <summary>
    /// Indicates that cancellation was requested, and the workflow should stop execution.
    /// </summary>
    public static readonly StepResult StoppedAfterCancel = new(WorkflowExitCodes.Cancelled, false, [new Error("Stopped after cancel.")]);

    /// <summary>
    /// Indicates that the step found no work left for the job: the workflow stops here, successfully.
    /// </summary>
    public static readonly StepResult NothingToDo = new(WorkflowExitCodes.Success, false, null);

    /// <summary>
    /// Indicates that the step failed with an error.
    /// </summary>
    public static StepResult Failed(Error error) => new(WorkflowExitCodes.Failed, false, [error]);

    /// <summary>
    /// Indicates that the step failed with multiple errors.
    /// </summary>
    public static StepResult Failed(IEnumerable<Error> errors) => new(WorkflowExitCodes.Failed, false, [.. errors]);

    /// <summary>
    /// Gets whether this result represents a failure.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Errors))]
    public bool IsFailure => ExitCode != WorkflowExitCodes.Success;

    /// <summary>
    /// Converts an error into a failed step result.
    /// </summary>
    public static implicit operator StepResult(Error error) => Failed(error);
}

/// <summary>
/// The result of a producing step.
/// </summary>
/// <typeparam name="T">The type of value the step produces.</typeparam>
public sealed class StepResult<T> : IProducedResult where T : notnull
{
    private StepResult(StepResult outcome, T? value)
    {
        Outcome = outcome;
        Value = value;
    }

    /// <summary>
    /// The outcome of the step.
    /// </summary>
    public StepResult Outcome { get; }

    /// <summary>
    /// The produced value, present when the step succeeded.
    /// </summary>
    public T? Value { get; }

    object? IProducedResult.Value => Value;

    /// <summary>
    /// Succeeds with the produced value.
    /// </summary>
    /// <param name="value">The value the step produced.</param>
    public static implicit operator StepResult<T>(T value) => new(StepResult.Successful, value);

    /// <summary>
    /// Carries a result that produced nothing: a failure, or a successful early stop.
    /// </summary>
    /// <param name="result">The valueless outcome.</param>
    public static implicit operator StepResult<T>(StepResult result) =>
        result is { IsFailure: false, Continue: true }
            ? throw new InvalidOperationException($"A step producing {typeof(T).Name} must return the value to succeed.")
            : new StepResult<T>(result, default);

    /// <summary>
    /// Converts an error into a failed step result.
    /// </summary>
    public static implicit operator StepResult<T>(Error error) => StepResult.Failed(error);
}
