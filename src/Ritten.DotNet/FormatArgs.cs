namespace Ritten.DotNet;

/// <summary>
/// Settings for a <see cref="IDotNet.CheckFormat"/> invocation.
/// </summary>
public record FormatArgs
{
    /// <summary>
    /// Whether to disable restoring packages on format.
    /// </summary>
    public bool NoRestore { get; init; }
}
