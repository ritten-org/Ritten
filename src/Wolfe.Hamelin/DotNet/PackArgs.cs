using Hamelin.FileSystem;

namespace Wolfe.Hamelin.DotNet;

/// <summary>
/// Arguments for a <see cref="IDotNet.Pack"/> invocation.
/// </summary>
public record PackArgs
{
    /// <summary>
    /// The project or solution to pack; when null, whatever the current directory resolves to.
    /// </summary>
    public string? Project { get; init; }

    /// <summary>
    /// The build configuration (e.g. <c>Release</c>); the SDK default when null.
    /// </summary>
    public string? Configuration { get; init; }

    /// <summary>
    /// Skips the implicit build, for pipelines with an explicit build step.
    /// </summary>
    public bool NoBuild { get; init; }

    /// <summary>
    /// The directory packages are written to; created if it doesn't exist.
    /// </summary>
    public required IDirectory Output { get; init; }
}
