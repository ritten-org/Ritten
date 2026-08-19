namespace Ritten.DotNet;

/// <summary>
/// Arguments for a <see cref="IDotNet.Restore"/> invocation.
/// </summary>
public record RestoreArgs
{
    /// <summary>
    /// The project or solution to restore; when null, whatever the current directory resolves to.
    /// </summary>
    public string? Project { get; init; }
}
