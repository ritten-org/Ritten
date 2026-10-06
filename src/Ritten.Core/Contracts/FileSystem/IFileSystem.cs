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

    /// <summary>
    /// Creates a new, empty directory in the system's temporary directory, outside the project.
    /// The caller must delete it.
    /// </summary>
    /// <param name="prefix">What the directory's name starts with, to say what made it.</param>
    /// <returns>The directory, created.</returns>
    IDirectory CreateTempDirectory(string prefix);
}
