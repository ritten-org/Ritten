namespace Ritten.Contracts;

/// <summary>
/// A step of a composed job.
/// </summary>
/// <param name="Name">The step's display name.</param>
/// <param name="Kind">What the step's outcome means.</param>
/// <param name="Produces">The type the step produces, or <c>null</c> for a non-producing step.</param>
/// <param name="Requires">The parameter types the step cannot run without.</param>
public sealed record JobStep(string Name, StepKind Kind, Type? Produces, IReadOnlyList<Type> Requires);
