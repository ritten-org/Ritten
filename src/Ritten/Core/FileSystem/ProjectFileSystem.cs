using Ritten.Contracts.FileSystem;

namespace Ritten.Core.FileSystem;

internal class ProjectFileSystem(RittenProject project) : IFileSystem
{
    /// <inheritdoc />
    public IDirectory ProjectRoot { get; } = new PhysicalDirectory(project.Directory);
}
