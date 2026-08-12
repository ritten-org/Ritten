using Hamelin.FileSystem;

namespace Wolfe.Hamelin.DotNet;

/// <summary>
/// The outcome of a <see cref="IDotNet.Pack"/> invocation.
/// </summary>
public record PackResult
{
    /// <summary>
    /// The <c>.nupkg</c> packages in the output directory after the pack.
    /// </summary>
    public required IReadOnlyList<IFile> Packages { get; init; }
}
