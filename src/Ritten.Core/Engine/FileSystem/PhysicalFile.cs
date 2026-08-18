using System.Diagnostics;
using Ritten.Contracts.FileSystem;

namespace Ritten.Engine.FileSystem;

[DebuggerDisplay("{Name} ({AbsolutePath})")]
internal class PhysicalFile(string path) : IFile
{
    public string Name { get; } = Path.GetFileName(path);
    public string NameWithoutExtension { get; } = Path.GetFileNameWithoutExtension(path);
    public string Extension { get; } = Path.GetExtension(path);
    public string AbsolutePath { get; } = Path.GetFullPath(path);
    public bool Exists => File.Exists(AbsolutePath);

    public void Delete()
    {
        if (!Exists) { return; }
        File.Delete(AbsolutePath);
    }

    public Stream OpenRead() => File.OpenRead(AbsolutePath);
    public Stream OpenWrite() => File.OpenWrite(AbsolutePath);

    public override string ToString() => AbsolutePath;
}
