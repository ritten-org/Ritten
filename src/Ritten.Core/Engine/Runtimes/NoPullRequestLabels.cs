using Ritten.Contracts;

namespace Ritten.Engine.Runtimes;

/// <summary>
/// The engine's own answer when nothing can read pull-request labels.
/// </summary>
internal sealed class NoPullRequestLabels : IPullRequestLabels
{
    /// <inheritdoc />
    public Task<IReadOnlyList<Label>?> Get(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Label>?>(null);
}
