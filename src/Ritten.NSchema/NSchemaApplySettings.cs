namespace Ritten.NSchema;

/// <summary>
/// How an apply treats the state lock and changes that would lose data.
/// </summary>
public sealed record NSchemaApplySettings
{
    /// <summary>
    /// Applies without taking the state lock (<c>--no-lock</c>).
    /// </summary>
    public bool NoLock { get; init; }

    /// <summary>
    /// The Destructive Action policy to take (<c>--destructive-actions</c>).
    /// </summary>
    public NSchemaDestructiveActions? DestructiveActions { get; init; }
}
