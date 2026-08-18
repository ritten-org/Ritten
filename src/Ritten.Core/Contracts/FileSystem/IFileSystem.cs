namespace Ritten.Contracts.FileSystem;

/// <summary>
/// Provides an abstraction for file system operations.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Gets the root of the project being built.
    /// </summary>
    IDirectory ProjectRoot { get; }

    /// <summary>
    /// Gets the directory build artifacts (e.g. packages) are written to.
    /// </summary>
    IDirectory Artifacts { get; }

    /// <summary>
    /// Gets the directory intermediate workflow output is written to.
    /// </summary>
    IDirectory Temp { get; }
}
