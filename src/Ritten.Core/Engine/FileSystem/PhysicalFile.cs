using System.Diagnostics;
using Ritten.Contracts.FileSystem;

namespace Ritten.Engine.FileSystem;

/// <summary>
/// A file on the physical file system.
/// </summary>
/// <param name="path">The path to the file, absolute or relative to the current directory.</param>
[DebuggerDisplay("{Name} ({AbsolutePath})")]
public class PhysicalFile(string path) : IFile
{
    /// <inheritdoc />
    public string Name { get; } = Path.GetFileName(path);

    /// <inheritdoc />
    public string NameWithoutExtension { get; } = Path.GetFileNameWithoutExtension(path);

    /// <inheritdoc />
    public string Extension { get; } = Path.GetExtension(path);

    /// <inheritdoc />
    public string AbsolutePath { get; } = Path.GetFullPath(path);

    /// <inheritdoc />
    public bool Exists => File.Exists(AbsolutePath);

    /// <inheritdoc />
    public void Delete()
    {
        if (!Exists) { return; }
        File.Delete(AbsolutePath);
    }

    /// <inheritdoc />
    public Stream OpenRead() => File.OpenRead(AbsolutePath);

    /// <inheritdoc />
    public Stream OpenWrite() => File.OpenWrite(AbsolutePath);

    /// <inheritdoc />
    public override string ToString() => AbsolutePath;
}
