namespace Ritten.Engine.Runs;

/// <summary>
/// Exposes an interface for running workflows.
/// </summary>
internal interface IWorkflowRunner
{
    /// <summary>
    /// Runs the current workflow.
    /// </summary>
    Task<WorkflowResult> Run(CancellationToken cancellationToken);
}
