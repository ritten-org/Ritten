namespace Ritten.Core.Runtimes;

/// <summary>
/// The fallback when no registered runtime matches.
/// </summary>
internal sealed class LocalRuntime : Runtime
{
    /// <inheritdoc />
    public override string Name => "local";

    /// <inheritdoc />
    public override IReadOnlyCollection<string> Markers { get; } = [];

    /// <inheritdoc />
    public override IReadOnlyCollection<string> Claims { get; } = [];

    /// <inheritdoc />
    public override void Configure(WorkflowRunBuilder builder, Func<string, string?> environment)
    {
    }
}
