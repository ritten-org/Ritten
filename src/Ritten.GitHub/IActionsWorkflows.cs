using Ritten.Contracts.FileSystem;
using Ritten.Engine;

namespace Ritten.GitHub;

/// <summary>
/// Reads and writes the GitHub Actions workflows of a repository.
/// </summary>
public interface IActionsWorkflows
{
    /// <summary>
    /// The repository's workflow files, wherever GitHub looks for them.
    /// </summary>
    /// <param name="repository">The root of the repository, which is the only place GitHub reads.</param>
    IEnumerable<IFile> Files(IDirectory repository);

    /// <summary>
    /// The file a workflow of the given name belongs in, whether or not it exists yet.
    /// </summary>
    /// <param name="repository">The root of the repository.</param>
    /// <param name="name">The file's name, without an extension.</param>
    IFile File(IDirectory repository, string name);

    /// <summary>
    /// Reads the given workflow file, failing rather than throwing when it isn't YAML.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<Result<ActionsWorkflow>> Read(IFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the workflow to the given file, replacing its contents.
    /// </summary>
    /// <param name="file">The file to write.</param>
    /// <param name="workflow">The workflow to write.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task Write(IFile file, ActionsWorkflow workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses the given workflow document.
    /// </summary>
    /// <param name="yaml">The document to parse.</param>
    Result<ActionsWorkflow> Parse(string yaml);

    /// <summary>
    /// Renders the given workflow as it would be written.
    /// </summary>
    /// <param name="workflow">The workflow to render.</param>
    string Render(ActionsWorkflow workflow);
}
