namespace Ritten.Contracts;

/// <summary>
/// The job being run.
/// </summary>
/// <param name="Workflow">The name of the workflow the job belongs to.</param>
/// <param name="Name">The name of the job, as given on the command line.</param>
/// <param name="DryRun">Whether this run is a rehearsal. Nothing that reaches outside the working directory happens.</param>
/// <param name="AutoApprove">Whether a job that would otherwise stop and ask has been approved up front.</param>
/// <param name="Force">Whether to redo work that's already in place, like reinstalling an installed tool.</param>
public sealed record WorkflowJob(string Workflow, string Name, bool DryRun = false, bool AutoApprove = false, bool Force = false);
