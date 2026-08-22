using Ritten.Contracts.FileSystem;

namespace Ritten.Engine;

/// <summary>
/// Reads and writes project files as documents.
/// </summary>
public interface IProjectFiles
{
    /// <summary>
    /// Reads the project file, failing rather than throwing when it isn't JSON.
    /// A file that isn't there reads as an empty document.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<Result<ProjectFile>> Read(IFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the document to the given file, replacing its contents.
    /// </summary>
    /// <param name="file">The file to write.</param>
    /// <param name="document">The document to write.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task Write(IFile file, ProjectFile document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses the given project file.
    /// </summary>
    /// <param name="json">The document to parse.</param>
    Result<ProjectFile> Parse(string json);

    /// <summary>
    /// Renders the given document as it would be written.
    /// </summary>
    /// <param name="document">The document to render.</param>
    string Render(ProjectFile document);
}
