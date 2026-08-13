namespace Ritten.Contracts;

/// <summary>
/// Represents a step in a pipeline that can be executed.
/// </summary>
public interface IPipelineStep
{
    /// <summary>
    /// Gets the name of the pipeline step.
    /// </summary>
    string Name { get => GetType().Name; }

    /// <summary>
    /// Runs the step in the pipeline.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The result of the step execution.</returns>
    Task<StepResult> Run(CancellationToken cancellationToken = default);
}
