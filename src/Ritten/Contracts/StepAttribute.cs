namespace Ritten.Contracts;

/// <summary>
/// Names and classifies a pipeline step.
/// </summary>
/// <param name="name">The step's display name.</param>
/// <param name="kind">What the step's outcome means.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class StepAttribute(string name, StepKind kind) : Attribute
{
    /// <summary>
    /// The step's display name.
    /// </summary>
    public string Name => name;

    /// <summary>
    /// What the step's outcome means.
    /// </summary>
    public StepKind Kind => kind;
}
