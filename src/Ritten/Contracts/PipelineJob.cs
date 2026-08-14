namespace Ritten.Contracts;

/// <summary>
/// The job being run.
/// </summary>
/// <param name="Pipeline">The name of the pipeline the job belongs to.</param>
/// <param name="Name">The name of the job, as given on the command line.</param>
public sealed record PipelineJob(string Pipeline, string Name);
