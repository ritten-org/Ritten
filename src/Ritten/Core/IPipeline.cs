namespace Ritten.Core;

/// <summary>
/// A pipeline: a named set of jobs a project can run.
/// </summary>
public interface IPipeline
{
    /// <summary>
    /// The name a <c>ritten.json</c> declares to select this pipeline, e.g. <c>dotnet-tool</c>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The human label, as the tool's output prints it, e.g. <c>dotnet tool</c>.
    /// </summary>
    string Label { get; }

    /// <summary>
    /// The pipeline's jobs.
    /// </summary>
    IReadOnlyList<IJob> Jobs { get; }
}
