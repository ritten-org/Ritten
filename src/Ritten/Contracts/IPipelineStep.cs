namespace Ritten.Contracts;

/// <summary>
/// A step in a pipeline.
/// </summary>
/// <remarks>
/// The interface carries only metadata: the step's behavior is a single public
/// <c>Run</c> method whose signature is its contract, like minimal APIs.
/// </remarks>
public interface IPipelineStep
{
    /// <summary>
    /// Gets the name of the pipeline step.
    /// </summary>
    string Name { get => GetType().Name; }

    /// <summary>
    /// Gets what this step's outcome means, for display and job-shape rules.
    /// </summary>
    StepKind Kind => StepKind.Work;
}
