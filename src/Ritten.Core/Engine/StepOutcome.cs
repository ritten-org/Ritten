using Ritten.Contracts;

namespace Ritten.Engine;

/// <summary>
/// A step paired with the result it ran to.
/// </summary>
/// <param name="Step">The step that ran.</param>
/// <param name="Result">The result the step returned.</param>
public record StepOutcome(Step Step, StepResult Result);
