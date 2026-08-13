namespace Ritten.DotNet;

/// <summary>
/// The outcome of a <see cref="IDotNet.Build"/> invocation.
/// </summary>
public record BuildResult
{
    /// <summary>
    /// True if the build succeeded, otherwise false.
    /// </summary>
    public required bool Succeeded { get; init; }

    /// <summary>
    /// The compiler and MSBuild diagnostics extracted from the build output,
    /// with duplicates from multi-targeted builds collapsed.
    /// </summary>
    public IReadOnlyList<DotNetDiagnostic> Diagnostics { get; init; } = [];
}
