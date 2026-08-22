using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Engine;

/// <summary>
/// Reports what the project file would say instead of writing it.
/// </summary>
internal sealed class DryRunProjectFiles(IWorkflowLog log, IProjectFiles inner) : IProjectFiles
{
    /// <inheritdoc />
    public Task<Result<ProjectFile>> Read(IFile file, CancellationToken cancellationToken = default) =>
        inner.Read(file, cancellationToken);

    /// <inheritdoc />
    public Task Write(IFile file, ProjectFile document, CancellationToken cancellationToken = default)
    {
        log.Skipped($"Would write {file.Name}:");
        log.Verbose(inner.Render(document));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Result<ProjectFile> Parse(string json) => inner.Parse(json);

    /// <inheritdoc />
    public string Render(ProjectFile document) => inner.Render(document);
}
