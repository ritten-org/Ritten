namespace Ritten.Contracts;

/// <summary>
/// The job being run.
/// </summary>
/// <param name="Pipeline">The name of the pipeline the job belongs to.</param>
/// <param name="Name">The name of the job, as given on the command line.</param>
/// <param name="DryRun">Whether this run is a rehearsal. Nothing that reaches outside the working directory happens.</param>
public sealed record PipelineJob(string Pipeline, string Name, bool DryRun = false);
