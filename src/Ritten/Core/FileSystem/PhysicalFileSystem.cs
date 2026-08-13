using Ritten.Contracts.FileSystem;

namespace Ritten.Core.FileSystem;

internal class PhysicalFileSystem(string path) : IFileSystem
{
    public IDirectory CurrentDirectory { get; } = new PhysicalDirectory(path);

    public IDirectory RootDirectory { get; } = ResolveRoot(path);

    private static PhysicalDirectory ResolveRoot(string path)
    {
        var absolutePath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(absolutePath)
                   ?? throw new ArgumentException("Path must have a root.", nameof(path));
        return new PhysicalDirectory(root);
    }
}
