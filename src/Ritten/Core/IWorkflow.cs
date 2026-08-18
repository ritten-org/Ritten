namespace Ritten.Core;

/// <summary>
/// A workflow: a named set of jobs a project can run.
/// </summary>
public interface IWorkflow
{
    /// <summary>
    /// The name a <c>ritten.json</c> declares to select this workflow, e.g. <c>dotnet-tool</c>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The human label, as the tool's output prints it, e.g. <c>dotnet tool</c>.
    /// </summary>
    string Label { get; }

    /// <summary>
    /// The workflow's jobs.
    /// </summary>
    IReadOnlyList<IJob> Jobs { get; }
}
