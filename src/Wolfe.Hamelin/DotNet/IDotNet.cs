using Hamelin.FileSystem;

namespace Wolfe.Hamelin.DotNet;

/// <summary>
/// Exposes functionality for interacting with .NET projects.
/// </summary>
public interface IDotNet
{
    /// <summary>
    /// Reads the package information from the given project file.
    /// </summary>
    Task<Project> ReadProject(IFile file, CancellationToken cancellationToken = default);
}
