using Ritten.Contracts.FileSystem;

namespace Ritten.DotNet;

/// <summary>
/// Settings for a <see cref="IDotNet.Test"/> invocation.
/// </summary>
public record TestArgs
{
    /// <summary>
    /// The project or solution to test; when null, whatever the current directory resolves to.
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
    /// The directory TRX results are written to and read back from; created if it doesn't exist.
    /// </summary>
    public required IDirectory ResultsDirectory { get; init; }

    /// <summary>
    /// Collects code coverage while the tests run, via the platform's code-coverage extension.
    /// </summary>
    public bool CollectCoverage { get; init; }
}
