using Ritten.Contracts.FileSystem;

namespace Ritten.Core.FileSystem;

internal class PhysicalFileSystem(string projectRoot) : IFileSystem
{
    public IDirectory ProjectRoot { get; } = new PhysicalDirectory(projectRoot);
}
