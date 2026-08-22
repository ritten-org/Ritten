namespace Ritten.Engine.Workflows;

/// <summary>
/// The workflow selected for a project directory.
/// </summary>
/// <param name="Workflow">The workflow the run is of.</param>
/// <param name="Project">The project file the workflow was chosen for.</param>
/// <param name="Recognised">Why this workflow was chosen.</param>
public sealed record SelectedWorkflow(IWorkflow Workflow, RittenProject Project, string? Recognised = null)
{
    /// <summary>
    /// Why the project declares no workflow.
    /// </summary>
    internal Error? MissingProjectReason { get; init; }
}
