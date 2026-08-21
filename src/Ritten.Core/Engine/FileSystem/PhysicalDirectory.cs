using System.Diagnostics;
using Microsoft.Extensions.FileSystemGlobbing;
using Ritten.Contracts.FileSystem;

namespace Ritten.Engine.FileSystem;

/// <summary>
/// A directory on the physical file system.
/// </summary>
/// <param name="path">The path to the directory, absolute or relative to the current directory.</param>
[DebuggerDisplay("{Name} ({AbsolutePath})")]
public class PhysicalDirectory(string path) : IDirectory
{
    /// <inheritdoc />
    public string Name { get; } = Path.GetFileName(path);

    /// <inheritdoc />
    public string AbsolutePath { get; } = Path.GetFullPath(path);

    /// <inheritdoc />
    public bool Exists => Directory.Exists(AbsolutePath);

    /// <inheritdoc />
    public void Create()
    {
        Directory.CreateDirectory(AbsolutePath);
    }

    /// <inheritdoc />
    public void Delete()
    {
        if (!Exists) { return; }
        Directory.Delete(AbsolutePath, true);
    }

    /// <inheritdoc />
    public IFile GetFile(string name) => new PhysicalFile(Path.Combine(AbsolutePath, name));

    /// <inheritdoc />
    public IDirectory GetDirectory(string name) => new PhysicalDirectory(Path.Combine(AbsolutePath, name));

    /// <inheritdoc />
    public IEnumerable<IFile> GetFiles(string searchPattern = "*")
    {
        var matcher = new Matcher();
        matcher.AddInclude(searchPattern);

        var paths = matcher.GetResultsInFullPath(AbsolutePath);
        return paths.Select(path => new PhysicalFile(path));
    }

    /// <inheritdoc />
    public IEnumerable<IDirectory> GetDirectories()
    {
        return Directory
            .EnumerateDirectories(AbsolutePath)
            .Select(path => new PhysicalDirectory(path));
    }

    /// <inheritdoc />
    public override string ToString() => AbsolutePath;
}
