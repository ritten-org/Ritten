using Ritten.Contracts.FileSystem;
using Ritten.Engine;
using Ritten.Reporting;

namespace Ritten.GitHub;

/// <summary>
/// Reports what a workflow file would say instead of writing it. Reading and parsing pass
/// through, so a rehearsal narrates the document it would have written in full.
/// </summary>
internal sealed class DryRunActionsWorkflows(IWorkflowLog log, IActionsWorkflows inner) : IActionsWorkflows
{
    /// <inheritdoc />
    public IEnumerable<IFile> Files(IDirectory repository) => inner.Files(repository);

    /// <inheritdoc />
    public IFile File(IDirectory repository, string name) => inner.File(repository, name);

    /// <inheritdoc />
    public Task<Result<ActionsWorkflow>> Read(IFile file, CancellationToken cancellationToken = default) =>
        inner.Read(file, cancellationToken);

    /// <inheritdoc />
    public Task Write(IFile file, ActionsWorkflow workflow, CancellationToken cancellationToken = default)
    {
        log.Skipped($"Would write {file.Name}:");
        log.Verbose(inner.Render(workflow));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Result<ActionsWorkflow> Parse(string yaml) => inner.Parse(yaml);

    /// <inheritdoc />
    public string Render(ActionsWorkflow workflow) => inner.Render(workflow);
}
