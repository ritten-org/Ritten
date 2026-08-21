namespace Ritten.DotNet;

/// <summary>
/// Settings for a <see cref="IDotNet.Format"/> invocation.
/// </summary>
public record FormatArgs
{
    /// <summary>
    /// Whether to report what isn't formatted rather than formatting it.
    /// </summary>
    public bool VerifyNoChanges { get; init; }

    /// <summary>
    /// Whether to disable restoring packages on format.
    /// </summary>
    public bool NoRestore { get; init; }
}
