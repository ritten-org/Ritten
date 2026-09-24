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
    public IDirectory Directory => new PhysicalDirectory(Path.GetDirectoryName(AbsolutePath) ?? AbsolutePath);

    /// <inheritdoc />
    public void Delete()
    {
        if (!Exists) { return; }
        File.Delete(AbsolutePath);
    }

    /// <inheritdoc />
    public Stream OpenRead() => File.OpenRead(AbsolutePath);

    /// <inheritdoc />
    public Stream OpenWrite() => new FileStream(AbsolutePath, FileMode.Create, FileAccess.Write);

    /// <inheritdoc />
    public void MoveTo(IFile destination) => File.Move(AbsolutePath, destination.AbsolutePath, overwrite: true);

    /// <inheritdoc />
    public UnixFileMode? GetUnixFileMode() =>
        OperatingSystem.IsWindows() || !Exists ? null : File.GetUnixFileMode(AbsolutePath);

    /// <inheritdoc />
    public void SetUnixFileMode(UnixFileMode mode)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(AbsolutePath, mode);
        }
    }

    /// <inheritdoc />
    public override string ToString() => AbsolutePath;
}
