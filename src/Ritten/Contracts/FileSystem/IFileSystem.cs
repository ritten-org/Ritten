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
}
