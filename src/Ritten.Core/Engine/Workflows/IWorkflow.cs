using Ritten.Contracts.FileSystem;

namespace Ritten.Engine.Workflows;

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

    /// <summary>
    /// Works out if this workflow is compatible with the project in the given directory.
    /// </summary>
    /// <param name="directory">The directory being set up.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The reason for claim compatibility, or null.</returns>
    Task<string?> IsCompatible(IDirectory directory, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
}
