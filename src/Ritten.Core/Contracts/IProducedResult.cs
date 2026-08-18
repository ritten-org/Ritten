namespace Ritten.Contracts;

/// <summary>
/// Lets the runner unwrap a <see cref="StepResult{T}"/> without knowing its type argument.
/// </summary>
internal interface IProducedResult
{
    /// <summary>
    /// The outcome of the step.
    /// </summary>
    StepResult Outcome { get; }

    /// <summary>
    /// The produced value, present when the step succeeded.
    /// </summary>
    object? Value { get; }
}
