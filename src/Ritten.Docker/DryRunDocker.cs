using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Docker;

/// <summary>
/// Reports actions/changes rather than doing them.
/// </summary>
internal sealed class DryRunDocker(IWorkflowLog log, IDocker inner) : IDocker
{
    /// <inheritdoc />
    public Task Build(IDirectory context, string tag, string? platform = null, CancellationToken ct = default) =>
        inner.Build(context, tag, platform, ct);

    /// <inheritdoc />
    public Task Tag(string source, string target, CancellationToken ct = default) =>
        inner.Tag(source, target, ct);

    /// <inheritdoc />
    public Task Push(string image, CancellationToken ct = default)
    {
        log.Skipped($"Would push {image}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Login(string registry, string username, string password, CancellationToken ct = default) =>
        inner.Login(registry, username, password, ct);

    /// <inheritdoc />
    public Task Run(ContainerRun run, CancellationToken ct = default)
    {
        log.Skipped($"Would run {run.Image} {string.Join(' ', run.Arguments)}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ComposeUp(IDirectory project, IReadOnlyDictionary<string, string>? environment = null, CancellationToken ct = default)
    {
        log.Skipped($"Would converge the stack in {project.AbsolutePath}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ComposeDown(IDirectory project, CancellationToken ct = default)
    {
        log.Skipped($"Would take down the stack in {project.AbsolutePath}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ComposeStop(IDirectory project, CancellationToken ct = default)
    {
        log.Skipped($"Would stop the stack in {project.AbsolutePath}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ComposeStart(IDirectory project, CancellationToken ct = default)
    {
        log.Skipped($"Would start the stack in {project.AbsolutePath}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ContainerState> Inspect(string container, CancellationToken ct = default) =>
        inner.Inspect(container, ct);
}
